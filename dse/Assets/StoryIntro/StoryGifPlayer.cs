using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryGifPlayer : MonoBehaviour
{
    public string resourcePath;
    public Sprite fallbackSprite;
    public bool playOnEnable = true;

    Image _image;
    List<GifFrame> _frames;
    int _frameIndex;
    float _timer;
    bool _isPlaying;

    void Awake()
    {
        _image = GetComponent<Image>();
        LoadFrames();
    }

    void OnEnable()
    {
        if (playOnEnable)
            PlayFromStart();
    }

    void Update()
    {
        if (!_isPlaying || _frames == null || _frames.Count <= 1) return;

        _timer += Time.unscaledDeltaTime;
        float delay = Mathf.Max(0.02f, _frames[_frameIndex].delay);

        if (_timer < delay) return;

        _timer -= delay;
        _frameIndex = (_frameIndex + 1) % _frames.Count;
        ApplyCurrentFrame();
    }

    public void Setup(string path, Sprite fallback)
    {
        resourcePath = path;
        fallbackSprite = fallback;
        _image = GetComponent<Image>();
        LoadFrames();
    }

    public void PlayFromStart()
    {
        _frameIndex = 0;
        _timer = 0f;
        _isPlaying = true;
        ApplyCurrentFrame();
    }

    public void Stop()
    {
        _isPlaying = false;
    }

    void LoadFrames()
    {
        if (_image == null) return;

        _frames = null;
        TextAsset gifBytes = Resources.Load<TextAsset>(resourcePath);

        if (gifBytes != null)
        {
            try
            {
                _frames = SimpleGifDecoder.Decode(gifBytes.bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not decode GIF '{resourcePath}': {ex.Message}");
            }
        }

        if ((_frames == null || _frames.Count == 0) && fallbackSprite != null)
            _image.sprite = fallbackSprite;
    }

    void ApplyCurrentFrame()
    {
        if (_image == null) return;

        if (_frames != null && _frames.Count > 0)
            _image.sprite = _frames[_frameIndex].sprite;
        else if (fallbackSprite != null)
            _image.sprite = fallbackSprite;
    }

    public class GifFrame
    {
        public Sprite sprite;
        public float delay;
    }

    static class SimpleGifDecoder
    {
        public static List<GifFrame> Decode(byte[] bytes)
        {
            var reader = new GifReader(bytes);
            string header = reader.ReadAscii(6);
            if (!header.StartsWith("GIF", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid GIF header.");

            int canvasWidth = reader.ReadUInt16();
            int canvasHeight = reader.ReadUInt16();
            byte packed = reader.ReadByte();
            bool hasGlobalColorTable = (packed & 0x80) != 0;
            int globalColorTableSize = 1 << ((packed & 0x07) + 1);
            reader.ReadByte();
            reader.ReadByte();

            Color32[] globalColorTable = hasGlobalColorTable
                ? reader.ReadColorTable(globalColorTableSize)
                : null;

            var frames = new List<GifFrame>();
            var canvas = new Color32[canvasWidth * canvasHeight];
            Clear(canvas, new RectInt(0, 0, canvasWidth, canvasHeight), canvasWidth);

            int disposal = 0;
            int delayCs = 8;
            bool hasTransparency = false;
            int transparentIndex = -1;

            while (!reader.EndOfData)
            {
                byte marker = reader.ReadByte();

                if (marker == 0x3B)
                    break;

                if (marker == 0x21)
                {
                    byte extensionLabel = reader.ReadByte();

                    if (extensionLabel == 0xF9)
                    {
                        reader.ReadByte();
                        byte gcePacked = reader.ReadByte();
                        disposal = (gcePacked >> 2) & 0x07;
                        hasTransparency = (gcePacked & 0x01) != 0;
                        delayCs = reader.ReadUInt16();
                        transparentIndex = reader.ReadByte();
                        reader.ReadByte();
                    }
                    else
                    {
                        reader.SkipSubBlocks();
                    }

                    continue;
                }

                if (marker != 0x2C)
                    throw new InvalidOperationException($"Unexpected GIF block 0x{marker:X2}.");

                int left = reader.ReadUInt16();
                int top = reader.ReadUInt16();
                int width = reader.ReadUInt16();
                int height = reader.ReadUInt16();
                byte imagePacked = reader.ReadByte();
                bool hasLocalColorTable = (imagePacked & 0x80) != 0;
                bool isInterlaced = (imagePacked & 0x40) != 0;
                int localColorTableSize = 1 << ((imagePacked & 0x07) + 1);
                Color32[] colorTable = hasLocalColorTable
                    ? reader.ReadColorTable(localColorTableSize)
                    : globalColorTable;

                if (colorTable == null)
                    throw new InvalidOperationException("GIF frame has no color table.");

                int minCodeSize = reader.ReadByte();
                byte[] compressed = reader.ReadSubBlocks();
                int[] indices = DecodeLzw(compressed, minCodeSize, width * height);
                if (isInterlaced)
                    indices = Deinterlace(indices, width, height);

                Color32[] previousCanvas = disposal == 3 ? Clone(canvas) : null;
                DrawFrame(canvas, canvasWidth, canvasHeight, left, top, width, height, indices, colorTable, hasTransparency, transparentIndex);
                frames.Add(CreateFrame(canvas, canvasWidth, canvasHeight, delayCs));

                if (disposal == 2)
                    Clear(canvas, new RectInt(left, top, width, height), canvasWidth);
                else if (disposal == 3 && previousCanvas != null)
                    canvas = previousCanvas;

                disposal = 0;
                delayCs = 8;
                hasTransparency = false;
                transparentIndex = -1;
            }

            return frames;
        }

        static GifFrame CreateFrame(Color32[] canvas, int width, int height, int delayCs)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color32[canvas.Length];
            for (int y = 0; y < height; y++)
            {
                int sourceRow = y * width;
                int targetRow = (height - 1 - y) * width;
                Array.Copy(canvas, sourceRow, pixels, targetRow, width);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return new GifFrame
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f)),
                delay = Mathf.Max(0.02f, delayCs / 100f)
            };
        }

        static void DrawFrame(
            Color32[] canvas,
            int canvasWidth,
            int canvasHeight,
            int left,
            int top,
            int width,
            int height,
            int[] indices,
            Color32[] colorTable,
            bool hasTransparency,
            int transparentIndex)
        {
            for (int y = 0; y < height; y++)
            {
                int destY = top + y;
                if (destY < 0 || destY >= canvasHeight) continue;

                for (int x = 0; x < width; x++)
                {
                    int destX = left + x;
                    if (destX < 0 || destX >= canvasWidth) continue;

                    int sourceIndex = y * width + x;
                    if (sourceIndex >= indices.Length) continue;

                    int colorIndex = indices[sourceIndex];
                    if (hasTransparency && colorIndex == transparentIndex) continue;
                    if (colorIndex < 0 || colorIndex >= colorTable.Length) continue;

                    var color = colorTable[colorIndex];
                    color.a = 255;
                    canvas[destY * canvasWidth + destX] = color;
                }
            }
        }

        static int[] DecodeLzw(byte[] data, int minCodeSize, int expectedSize)
        {
            int clearCode = 1 << minCodeSize;
            int endCode = clearCode + 1;
            int codeSize = minCodeSize + 1;
            int bitPosition = 0;
            int[] previous = null;
            var output = new List<int>(expectedSize);
            var dictionary = CreateDictionary(clearCode);

            while (true)
            {
                int code = ReadCode(data, ref bitPosition, codeSize);
                if (code < 0) break;

                if (code == clearCode)
                {
                    dictionary = CreateDictionary(clearCode);
                    codeSize = minCodeSize + 1;
                    previous = null;
                    continue;
                }

                if (code == endCode)
                    break;

                int[] entry;
                if (code < dictionary.Count && dictionary[code] != null)
                    entry = dictionary[code];
                else if (code == dictionary.Count && previous != null)
                    entry = Append(previous, previous[0]);
                else
                    break;

                output.AddRange(entry);
                if (output.Count >= expectedSize)
                    break;

                if (previous != null && dictionary.Count < 4096)
                {
                    dictionary.Add(Append(previous, entry[0]));
                    if (dictionary.Count == (1 << codeSize) && codeSize < 12)
                        codeSize++;
                }

                previous = entry;
            }

            if (output.Count < expectedSize)
            {
                int missing = expectedSize - output.Count;
                for (int i = 0; i < missing; i++)
                    output.Add(0);
            }

            if (output.Count > expectedSize)
                output.RemoveRange(expectedSize, output.Count - expectedSize);

            return output.ToArray();
        }

        static List<int[]> CreateDictionary(int clearCode)
        {
            var dictionary = new List<int[]>(4096);
            for (int i = 0; i < clearCode; i++)
                dictionary.Add(new[] { i });
            dictionary.Add(null);
            dictionary.Add(null);
            return dictionary;
        }

        static int ReadCode(byte[] data, ref int bitPosition, int codeSize)
        {
            int code = 0;
            for (int i = 0; i < codeSize; i++)
            {
                int byteIndex = bitPosition >> 3;
                if (byteIndex >= data.Length)
                    return -1;

                int bitIndex = bitPosition & 7;
                if ((data[byteIndex] & (1 << bitIndex)) != 0)
                    code |= 1 << i;

                bitPosition++;
            }

            return code;
        }

        static int[] Append(int[] source, int value)
        {
            var result = new int[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[result.Length - 1] = value;
            return result;
        }

        static int[] Deinterlace(int[] interlaced, int width, int height)
        {
            var result = new int[interlaced.Length];
            int sourceRow = 0;
            int[] starts = { 0, 4, 2, 1 };
            int[] steps = { 8, 8, 4, 2 };

            for (int pass = 0; pass < 4; pass++)
            {
                for (int y = starts[pass]; y < height; y += steps[pass])
                {
                    if (sourceRow >= height) return result;
                    Array.Copy(interlaced, sourceRow * width, result, y * width, width);
                    sourceRow++;
                }
            }

            return result;
        }

        static Color32[] Clone(Color32[] source)
        {
            var clone = new Color32[source.Length];
            Array.Copy(source, clone, source.Length);
            return clone;
        }

        static void Clear(Color32[] canvas, RectInt rect, int canvasWidth)
        {
            var transparent = new Color32(0, 0, 0, 0);
            for (int y = rect.y; y < rect.y + rect.height; y++)
            {
                for (int x = rect.x; x < rect.x + rect.width; x++)
                    canvas[y * canvasWidth + x] = transparent;
            }
        }
    }

    class GifReader
    {
        readonly byte[] _bytes;
        int _position;

        public bool EndOfData => _position >= _bytes.Length;

        public GifReader(byte[] bytes)
        {
            _bytes = bytes;
        }

        public byte ReadByte()
        {
            if (_position >= _bytes.Length)
                throw new InvalidOperationException("Unexpected end of GIF data.");
            return _bytes[_position++];
        }

        public int ReadUInt16()
        {
            int lo = ReadByte();
            int hi = ReadByte();
            return lo | (hi << 8);
        }

        public string ReadAscii(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = (char)ReadByte();
            return new string(chars);
        }

        public Color32[] ReadColorTable(int size)
        {
            var table = new Color32[size];
            for (int i = 0; i < size; i++)
                table[i] = new Color32(ReadByte(), ReadByte(), ReadByte(), 255);
            return table;
        }

        public byte[] ReadSubBlocks()
        {
            var data = new List<byte>();

            while (true)
            {
                int size = ReadByte();
                if (size == 0) break;

                for (int i = 0; i < size; i++)
                    data.Add(ReadByte());
            }

            return data.ToArray();
        }

        public void SkipSubBlocks()
        {
            while (true)
            {
                int size = ReadByte();
                if (size == 0) break;
                _position += size;
                if (_position > _bytes.Length)
                    throw new InvalidOperationException("Unexpected end of GIF data.");
            }
        }
    }
}

let currentRole = '';
let currentAuthMode = 'login'; // 'login' ou 'register'

function showLogin(role) {
    currentRole = role;
    
    const loginTitle = document.getElementById('login-title');
    const submitBtn = document.getElementById('submit-btn');
    const authTabs = document.getElementById('auth-tabs');
    const tabLogin = document.getElementById('tab-login');

    // Reseta sempre para a aba "Entrar" ao abrir a tela
    switchAuthMode('login');

    if (role === 'aluno') {
        loginTitle.textContent = 'Acesso do Aluno';
        loginTitle.style.color = 'var(--color-orange)';
        submitBtn.style.backgroundColor = 'var(--color-orange)';
        
        // Colore a aba ativa de laranja
        document.documentElement.style.setProperty('--color-active-tab', 'var(--color-orange)');
        tabLogin.style.backgroundColor = 'var(--color-orange)';
        
        // Aluno pode criar conta, então mostramos as abas
        authTabs.style.display = 'flex';
    } else {
        loginTitle.textContent = 'Acesso do Professor';
        loginTitle.style.color = 'var(--color-red)';
        submitBtn.style.backgroundColor = 'var(--color-red)';
        
        // Colore a aba ativa de vermelho (caso o CSS utilize a variável no futuro)
        document.documentElement.style.setProperty('--color-active-tab', 'var(--color-red)');
        tabLogin.style.backgroundColor = 'var(--color-red)';
        
        // Professor NÃO cria conta aqui, escondemos as abas
        authTabs.style.display = 'none';
    }

    document.getElementById('selection-screen').classList.remove('active');
    document.getElementById('selection-screen').classList.add('hidden');
    
    document.getElementById('login-screen').classList.remove('hidden');
    document.getElementById('login-screen').classList.add('active');
}

function showSelection() {
    currentRole = '';
    document.getElementById('login-screen').classList.remove('active');
    document.getElementById('login-screen').classList.add('hidden');
    
    document.getElementById('selection-screen').classList.remove('hidden');
    document.getElementById('selection-screen').classList.add('active');
    
    document.getElementById('login-form').reset();
}

// Alterna entre as abas Entrar / Criar Conta
function switchAuthMode(mode) {
    currentAuthMode = mode;
    const tabLogin = document.getElementById('tab-login');
    const tabRegister = document.getElementById('tab-register');
    const registerFields = document.querySelectorAll('.register-only');
    const submitBtn = document.getElementById('submit-btn');
    const forgotPasswordContainer = document.getElementById('forgot-password-container');

    // Cor baseada no papel
    const activeColor = currentRole === 'aluno' ? 'var(--color-orange)' : 'var(--color-blue)';

    if (mode === 'login') {
        tabLogin.classList.add('active');
        tabLogin.style.backgroundColor = activeColor;
        tabRegister.classList.remove('active');
        tabRegister.style.backgroundColor = 'transparent';
        
        registerFields.forEach(el => el.classList.add('hidden-field'));
        forgotPasswordContainer.style.display = 'block';
        submitBtn.textContent = 'Entrar';
    } else {
        tabRegister.classList.add('active');
        tabRegister.style.backgroundColor = activeColor;
        tabLogin.classList.remove('active');
        tabLogin.style.backgroundColor = 'transparent';
        
        registerFields.forEach(el => el.classList.remove('hidden-field'));
        forgotPasswordContainer.style.display = 'none'; // Não faz sentido esquecer senha no cadastro
        submitBtn.textContent = 'Criar Conta';
    }
}

// Mostra/Oculta Senha trocando o SVG
function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    const svgOpen = `<svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>`;
    const svgClosed = `<svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>`;

    if (input.type === 'password') {
        input.type = 'text';
        button.innerHTML = svgClosed; // Desenho do olho cortado
    } else {
        input.type = 'password';
        button.innerHTML = svgOpen;   // Desenho do olho aberto
    }
}

function handleLogin(event) {
    event.preventDefault();
    const email = document.getElementById('email').value;
    
    sessionStorage.setItem('userEmail', email);

    if (currentRole === 'aluno') {
        window.location.href = 'aluno.html';
    } else {
        window.location.href = 'professor.html';
    }
}
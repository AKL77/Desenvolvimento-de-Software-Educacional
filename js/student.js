// Recupera o email salvo no login e exibe no modal
document.addEventListener('DOMContentLoaded', () => {
    const savedEmail = sessionStorage.getItem('userEmail');
    if (savedEmail) {
        document.getElementById('display-user-email').textContent = savedEmail;
    }
});

function openJoinModal() {
    document.getElementById('join-modal').classList.remove('hidden');
    document.getElementById('class-code').value = ''; 
    validateCodeInput(); 
}

function closeJoinModal() {
    document.getElementById('join-modal').classList.add('hidden');
}

function validateCodeInput() {
    const input = document.getElementById('class-code').value;
    const btn = document.getElementById('submit-code-btn');
    
    if (input.trim().length > 0) {
        btn.classList.remove('disabled');
        btn.classList.add('active');
        btn.removeAttribute('disabled');
    } else {
        btn.classList.add('disabled');
        btn.classList.remove('active');
        btn.setAttribute('disabled', 'true');
    }
}
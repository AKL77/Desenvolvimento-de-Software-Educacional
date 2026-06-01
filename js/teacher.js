function openCreateModal() {
    document.getElementById('create-modal').classList.remove('hidden');
    document.getElementById('new-class-name').value = ''; 
    document.getElementById('new-class-school').value = ''; 
    validateCreateInput(); 
}

function closeCreateModal() {
    document.getElementById('create-modal').classList.add('hidden');
}

function validateCreateInput() {
    const input = document.getElementById('new-class-name').value;
    const btn = document.getElementById('submit-create-btn');
    
    if (input.trim().length > 0) {
        btn.classList.remove('disabled');
        btn.classList.add('active-red');
        btn.removeAttribute('disabled');
        btn.style.backgroundColor = ''; 
    } else {
        btn.classList.add('disabled');
        btn.classList.remove('active-red');
        btn.setAttribute('disabled', 'true');
        btn.style.backgroundColor = '#cccccc'; 
    }
}
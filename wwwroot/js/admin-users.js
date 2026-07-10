let deleteUserId = null;

function resetUserForm() {
    document.getElementById('userForm').reset();
    let idInput = document.getElementById('userId');
    if (idInput) idInput.remove();
    document.getElementById('userModalTitle').textContent = 'Create User';
    document.getElementById('passwordFields').style.display = 'block';
    document.getElementById('confirmPasswordField').style.display = 'block';
    document.getElementById('Password').required = true;
    document.getElementById('ConfirmPassword').required = true;
    let roleSelect = document.getElementById('Role');
    let adminOption = roleSelect.querySelector('option[value="Admin"]');
    if (adminOption) adminOption.remove();
    clearFieldErrors();
}

function editUser(id) {
    clearFieldErrors();
    fetch('/Admin/Users/GetUser/' + id)
        .then(r => r.json())
        .then(u => {
            let existingInput = document.getElementById('userId');
            if (!existingInput) {
                let input = document.createElement('input');
                input.type = 'hidden';
                input.id = 'userId';
                input.name = 'Id';
                document.getElementById('userForm').appendChild(input);
            }
            document.getElementById('userId').value = u.id;
            document.getElementById('FirstName').value = u.firstName;
            document.getElementById('LastName').value = u.lastName;
            document.getElementById('Email').value = u.email;
            document.getElementById('UserName').value = u.userName;
            let roleSelect = document.getElementById('Role');
            let adminOption = roleSelect.querySelector('option[value="Admin"]');
            if (!adminOption) {
                adminOption = document.createElement('option');
                adminOption.value = 'Admin';
                adminOption.textContent = 'Admin';
                roleSelect.prepend(adminOption);
            }
            roleSelect.value = u.role;
            document.getElementById('userModalTitle').textContent = 'Edit User';
            document.getElementById('passwordFields').style.display = 'none';
            document.getElementById('confirmPasswordField').style.display = 'none';
            document.getElementById('Password').required = false;
            document.getElementById('ConfirmPassword').required = false;
            new bootstrap.Modal(document.getElementById('userModal')).show();
        });
}

function clearFieldErrors() {
    document.querySelectorAll('#userForm .is-invalid').forEach(el => el.classList.remove('is-invalid'));
    document.querySelectorAll('#userForm .invalid-feedback').forEach(el => el.remove());
    document.getElementById('userFormErrors').textContent = '';
}

document.getElementById('userForm').addEventListener('submit', function (e) {
    e.preventDefault();
    clearFieldErrors();
    let form = this;
    let formData = new FormData(form);
    let idInput = document.getElementById('userId');
    let url = idInput ? '/Admin/Users/Edit' : '/Admin/Users/Create';

    fetch(url, { method: 'POST', body: formData })
        .then(r => r.json())
        .then(result => {
            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
                showAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                document.getElementById('userFormErrors').textContent = result.error || 'Validation failed.';
            }
        })
        .catch(err => {
            document.getElementById('userFormErrors').textContent = 'Error: ' + err.message;
        });
});

function confirmDelete(id, userName) {
    deleteUserId = id;
    document.getElementById('deleteUserName').textContent = userName;
    new bootstrap.Modal(document.getElementById('deleteModal')).show();
}

document.getElementById('confirmDeleteBtn').addEventListener('click', function () {
    if (!deleteUserId) return;
    fetch('/Admin/Users/Delete/' + deleteUserId, { method: 'POST' })
        .then(r => r.json())
        .then(result => {
            bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
            if (result.success) {
                showAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                showAlert('danger', result.message);
            }
        });
});

function showAlert(type, message) {
    let placeholder = document.getElementById('alertPlaceholder');
    placeholder.innerHTML = '<div class="alert alert-' + type + ' alert-dismissible fade show">' + message + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
}

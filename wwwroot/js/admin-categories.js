let deleteCatId = null;

document.getElementById('categoryForm').addEventListener('submit', function (e) {
    e.preventDefault();
    let formData = new FormData(this);
    fetch('/Admin/Categories/Create', { method: 'POST', body: formData })
        .then(r => r.json())
        .then(result => {
            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('categoryModal')).hide();
                showCatAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                document.getElementById('catFormErrors').textContent = result.error;
            }
        });
});

function confirmDeleteCat(id, name) {
    deleteCatId = id;
    document.getElementById('deleteCatName').textContent = name;
    new bootstrap.Modal(document.getElementById('deleteCatModal')).show();
}

document.getElementById('confirmDeleteCatBtn').addEventListener('click', function () {
    if (!deleteCatId) return;
    fetch('/Admin/Categories/Delete/' + deleteCatId, { method: 'POST' })
        .then(r => r.json())
        .then(result => {
            bootstrap.Modal.getInstance(document.getElementById('deleteCatModal')).hide();
            if (result.success) {
                showCatAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                showCatAlert('danger', result.message);
            }
        });
});

function showCatAlert(type, message) {
    let placeholder = document.getElementById('alertPlaceholder');
    placeholder.innerHTML = '<div class="alert alert-' + type + ' alert-dismissible fade show">' + message + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
}

let deleteBorrowerId = null;

function resetBorrowerForm() {
    document.getElementById('borrowerForm').reset();
    document.getElementById('borrowerId').value = '0';
    document.getElementById('borrowerCreatedAt').value = '';
    document.getElementById('borrowerModalTitle').textContent = 'Add Borrower';
    document.getElementById('borrowerFormErrors').textContent = '';
    document.getElementById('borrowerIsActive').checked = true;
    document.getElementById('borrowerBarcodeDisplay').style.display = 'none';
}

function editBorrower(id) {
    document.getElementById('borrowerFormErrors').textContent = '';
    fetch('/Librarian/Borrowers/GetBorrower/' + id)
        .then(r => r.json())
        .then(b => {
            document.getElementById('borrowerId').value = b.id;
            document.getElementById('borrowerCreatedAt').value = b.createdAt;
            document.getElementById('borrowerBarcodeText').textContent = b.barcode;
            document.getElementById('borrowerBarcodeDisplay').style.display = 'block';
            document.getElementById('borrowerFirstName').value = b.firstName;
            document.getElementById('borrowerLastName').value = b.lastName;
            document.getElementById('borrowerEmail').value = b.email || '';
            document.getElementById('borrowerPhone').value = b.phone || '';
            document.getElementById('borrowerGrade').value = b.grade || '';
            document.getElementById('borrowerIDNumber').value = b.idNumber || '';
            document.getElementById('borrowerAddress').value = b.address || '';
            document.getElementById('borrowerIsActive').checked = b.isActive;
            document.getElementById('borrowerModalTitle').textContent = 'Edit Borrower';
            new bootstrap.Modal(document.getElementById('borrowerModal')).show();
        });
}

document.getElementById('borrowerForm').addEventListener('submit', function (e) {
    e.preventDefault();
    let form = this;
    let formData = new FormData(form);
    let id = parseInt(document.getElementById('borrowerId').value);
    let url = id > 0 ? '/Librarian/Borrowers/Edit' : '/Librarian/Borrowers/Create';
    if (!document.getElementById('borrowerIsActive').checked) formData.append('IsActive', 'false');

    fetch(url, { method: 'POST', body: formData })
        .then(r => r.json())
        .then(result => {
            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('borrowerModal')).hide();
                showBorrowerAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                document.getElementById('borrowerFormErrors').textContent = result.error || 'Validation failed.';
            }
        });
});

function confirmDeleteBorrower(id, name) {
    deleteBorrowerId = id;
    document.getElementById('deleteBorrowerName').textContent = name;
    new bootstrap.Modal(document.getElementById('deleteBorrowerModal')).show();
}

document.getElementById('confirmDeleteBorrowerBtn').addEventListener('click', function () {
    if (!deleteBorrowerId) return;
    fetch('/Librarian/Borrowers/Delete/' + deleteBorrowerId, { method: 'POST' })
        .then(r => r.json())
        .then(result => {
            bootstrap.Modal.getInstance(document.getElementById('deleteBorrowerModal')).hide();
            if (result.success) {
                showBorrowerAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                showBorrowerAlert('danger', result.message);
            }
        });
});

function showBorrowerAlert(type, message) {
    let placeholder = document.getElementById('alertPlaceholder');
    placeholder.innerHTML = '<div class="alert alert-' + type + ' alert-dismissible fade show">' + message + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
}

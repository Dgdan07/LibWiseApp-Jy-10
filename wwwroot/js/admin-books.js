let deleteBookId = null;

function resetBookForm() {
    document.getElementById('bookForm').reset();
    document.getElementById('bookId').value = '0';
    document.getElementById('bookCreatedAt').value = '';
    document.getElementById('bookModalTitle').textContent = 'Add Book';
    document.getElementById('bookFormErrors').textContent = '';
    document.getElementById('bookIsActive').checked = true;
}

function editBook(id) {
    document.getElementById('bookFormErrors').textContent = '';
    fetch('/Admin/Books/GetBook/' + id)
        .then(r => r.json())
        .then(b => {
            document.getElementById('bookId').value = b.id;
            document.getElementById('bookCreatedAt').value = b.createdAt;
            document.getElementById('bookISBN').value = b.isbn || '';
            document.getElementById('bookTitle').value = b.title;
            document.getElementById('bookAuthor').value = b.author;
            document.getElementById('bookPublisher').value = b.publisher || '';
            document.getElementById('bookCategoryId').value = b.categoryId || '';
            document.getElementById('bookPublicationYear').value = b.publicationYear;
            document.getElementById('bookTotalCopies').value = b.totalCopies;
            document.getElementById('bookAvailableCopies').value = b.availableCopies;
            document.getElementById('bookShelfLocation').value = b.shelfLocation || '';
            document.getElementById('bookDescription').value = b.description || '';
            document.getElementById('bookIsActive').checked = b.isActive;
            document.getElementById('bookModalTitle').textContent = 'Edit Book';
            new bootstrap.Modal(document.getElementById('bookModal')).show();
        });
}

document.getElementById('bookForm').addEventListener('submit', function (e) {
    e.preventDefault();
    let form = this;
    let formData = new FormData(form);
    let id = parseInt(document.getElementById('bookId').value);
    let url = id > 0 ? '/Admin/Books/Edit' : '/Admin/Books/Create';
    if (!document.getElementById('bookIsActive').checked) formData.append('IsActive', 'false');

    fetch(url, { method: 'POST', body: formData })
        .then(r => r.json())
        .then(result => {
            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('bookModal')).hide();
                showBookAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                document.getElementById('bookFormErrors').textContent = result.error || 'Validation failed.';
            }
        });
});

function confirmDeleteBook(id, title) {
    deleteBookId = id;
    document.getElementById('deleteBookTitle').textContent = title;
    new bootstrap.Modal(document.getElementById('deleteBookModal')).show();
}

document.getElementById('confirmDeleteBookBtn').addEventListener('click', function () {
    if (!deleteBookId) return;
    fetch('/Admin/Books/Delete/' + deleteBookId, { method: 'POST' })
        .then(r => r.json())
        .then(result => {
            bootstrap.Modal.getInstance(document.getElementById('deleteBookModal')).hide();
            if (result.success) {
                showBookAlert('success', result.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                showBookAlert('danger', result.message);
            }
        });
});

function showBookAlert(type, message) {
    let placeholder = document.getElementById('alertPlaceholder');
    placeholder.innerHTML = '<div class="alert alert-' + type + ' alert-dismissible fade show">' + message + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
}

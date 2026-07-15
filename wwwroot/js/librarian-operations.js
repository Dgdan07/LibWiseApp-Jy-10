// === Returns Module ===
let returnsTimeout;

function initReturns() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(returnsTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            returnsTimeout = setTimeout(() => returnsLookupBorrower(val), 400);
        }
    });

    $('#searchBorrower').on('input', function() {
        clearTimeout(returnsTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            returnsTimeout = setTimeout(() => returnsSearchBorrowerByName(val), 400);
        } else {
            $('#borrowerResult').html('');
        }
    });
}

function returnsSearchBorrowerByName(term) {
    $.getJSON('/Librarian/Returns/SearchBorrower', { term: term }, function(data) {
        if (data.length === 0) {
            $('#borrowerResult').html('<div class="text-muted">No matches.</div>');
        } else {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="returnsSelectBorrower('${b.barcode}')">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        }
    });
}

function returnsSelectBorrower(barcode) {
    $('#borrowerResult').html('');
    $('#searchBorrower').val('');
    $('#barcodeInput').val(barcode);
    returnsLookupBorrower(barcode);
}

function returnsLookupBorrower(barcode) {
    $('#borrowerInfo').html('<span class="text-primary"><i class="bi bi-hourglass"></i> Searching...</span>');

    $.post('/Librarian/Returns/Process', { borrowerBarcode: barcode }, function(res) {
        if (res.success) {
            $('#borrowerInfo').html(`<span class="text-success"><i class="bi bi-person-check"></i> <strong>${res.borrowerName}</strong></span>`);

            function canExtend(wasExtended, dueDate) {
                if (wasExtended) return false;
                const now = new Date();
                const due = new Date(dueDate);
                const oneDayBefore = new Date(due);
                oneDayBefore.setDate(oneDayBefore.getDate() - 1);
                return now.toDateString() === oneDayBefore.toDateString();
            }

            let html = '<ul class="list-group">';
            res.books.forEach(b => {
                const due = new Date(b.dueDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
                const showExtend = canExtend(b.wasExtended, b.dueDate);
                html += `<li class="list-group-item d-flex justify-content-between align-items-center">
                    <div><strong>${b.bookTitle}</strong><br/>
                    <small class="text-muted">Borrowed: ${new Date(b.borrowedAt).toLocaleDateString()} | Due: ${due}</small></div>
                    <div class="d-flex gap-1">
                        ${showExtend ? `<button class="btn btn-warning btn-sm" onclick="extendBorrow(${b.id})"><i class="bi bi-clock"></i> Extend</button>` : ''}
                        <button class="btn btn-success btn-sm" onclick="returnBook(${b.id}, '${b.bookTitle.replace(/'/g, "\\'")}')">
                            <i class="bi bi-arrow-return-left"></i> Return
                        </button>
                    </div>
                </li>`;
            });
            html += '</ul>';
            $('#booksList').html(html);
        } else {
            $('#borrowerInfo').html(`<span class="text-danger"><i class="bi bi-exclamation-circle"></i> ${res.message}</span>`);
            $('#booksList').html('<p class="text-muted mb-0">No active borrowings.</p>');
        }
    }).fail(function() {
        $('#borrowerInfo').html('<span class="text-danger">Request failed.</span>');
    });
}

function extendBorrow(recordId) {
    if (!confirm('Extend this borrowing by 2 days?')) return;

    $.post('/Librarian/Returns/Extend', { recordId: recordId }, function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            resetReturnsView();
        } else {
            showToast(res.message, 'danger');
        }
    });
}

function returnBook(recordId, title) {
    if (!confirm(`Return "${title}"?`)) return;

    $.post('/Librarian/Returns/ReturnBook', { recordId: recordId }, function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            resetReturnsView();
        } else {
            showToast('Error: ' + res.message, 'danger');
        }
    }).fail(function() {
        showToast('Request failed.', 'danger');
    });
}

function showResultAlert(type, message) {
    $('#resultAlert').html(`<div class="alert alert-${type}">${message}</div>`).show();
    setTimeout(() => $('#resultAlert').hide(), 5000);
}

function resetReturnsView() {
    $('#barcodeInput').val('').focus();
    $('#searchBorrower').val('');
    $('#borrowerResult').html('');
    $('#booksList').html('<p class="text-muted mb-0">Scan a borrower to see their borrowed books.</p>');
    $('#borrowerInfo').html('<span class="text-muted small">Awaiting scan...</span>');
}

// === Borrowing Module ===
let borrowerTimeout, bookTimeout;

function initBorrowing() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(borrowerTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            borrowerTimeout = setTimeout(() => lookupBorrower(val), 400);
        }
    });

    $('#searchBorrower').on('input', function() {
        clearTimeout(borrowerTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            borrowerTimeout = setTimeout(() => searchBorrower(val), 400);
        }
    });

    $('#searchBook').on('input', function() {
        clearTimeout(bookTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            bookTimeout = setTimeout(() => searchBooks(val), 400);
        } else {
            loadAvailableBooks();
        }
    });

    $('#borrowBtn').click(function() {
        const borrowerBarcode = $('#borrowerBarcode').val();
        const bookId = $('#bookId').val();
        if (!borrowerBarcode || !bookId) return;

        $(this).prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Processing...');

        $.post('/Librarian/Borrowing/Process', { borrowerBarcode: borrowerBarcode, bookId: bookId }, function(res) {
            if (res.success) {
                showToast(res.message, 'success');
                location.reload();
            } else {
                showToast('Error: ' + res.message, 'danger');
                $('#borrowBtn').prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Confirm Borrowing');
            }
        }).fail(function() {
            showToast('Request failed.', 'danger');
            $('#borrowBtn').prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Confirm Borrowing');
        });
    });
}

function lookupBorrower(barcode) {
    $.getJSON('/Librarian/Borrowing/SearchBorrower', { term: barcode }, function(data) {
        if (data.length === 1 && data[0].barcode === barcode) {
            selectBorrower(data[0]);
        } else if (data.length > 0) {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="selectBorrower(${JSON.stringify(b).replace(/"/g, "'")})">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        } else {
            $('#borrowerResult').html('<div class="text-danger">Borrower not found.</div>');
        }
    });
}

function searchBorrower(term) {
    $.getJSON('/Librarian/Borrowing/SearchBorrower', { term: term }, function(data) {
        if (data.length === 0) {
            $('#borrowerResult').html('<div class="text-muted">No matches.</div>');
        } else {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="selectBorrower(${JSON.stringify(b).replace(/"/g, "'")})">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        }
    });
}

function selectBorrower(b) {
    $('#borrowerId').val(b.id);
    $('#borrowerBarcode').val(b.barcode);
    $('#borrowerResult').html(`<div class="alert alert-success py-2 mb-0">
        <i class="bi bi-check-circle"></i> <strong>${b.name}</strong> (${b.barcode})</div>`);
    $('#bookLockedNotice').hide();
    $('#bookSearchSection').show();
    unlockAvailableBooks();
}

function unlockAvailableBooks() {
    $('#booksLockedOverlay').hide();
    $('#availableBooksList').show();
    loadAvailableBooks();
}

function loadAvailableBooks() {
    $.getJSON('/Librarian/Borrowing/GetAvailableBooks', function(data) {
        $('#bookCount').text(data.length + ' books');
        if (data.length === 0) {
            $('#availableBooksList').html('<div class="text-center text-muted py-4">No available books.</div>');
        } else {
            let html = '<ul class="list-group list-group-flush">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action py-2" onclick="selectBook(${JSON.stringify(b).replace(/"/g, "'")})">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${b.title}</strong><br/>
                            <small class="text-muted">${b.author} ${b.isbn ? '| ISBN: ' + b.isbn : ''}</small>
                        </div>
                        <span class="badge bg-success">${b.availableCopies}</span>
                    </div>
                </li>`;
            });
            html += '</ul>';
            $('#availableBooksList').html(html);
        }
    });
}

function searchBooks(term) {
    $.getJSON('/Librarian/Borrowing/SearchBook', { term: term }, function(data) {
        if (data.length === 0) {
            $('#availableBooksList').html('<div class="text-center text-muted py-4">No available books found.</div>');
        } else {
            let html = '<ul class="list-group list-group-flush">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action py-2" onclick="selectBook(${JSON.stringify(b).replace(/"/g, "'")})">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${b.title}</strong><br/>
                            <small class="text-muted">${b.author} ${b.isbn ? '| ISBN: ' + b.isbn : ''}</small>
                        </div>
                        <span class="badge bg-success">${b.availableCopies}</span>
                    </div>
                </li>`;
            });
            html += '</ul>';
            $('#availableBooksList').html(html);
        }
    });
}

function selectBook(b) {
    if (!$('#borrowerId').val()) return;
    $('#bookId').val(b.id);
    $('#bookResult').html(`<div class="alert alert-success py-2 mb-0" style="cursor:pointer;" onclick="deselectBook()" title="Click to choose a different book">
        <i class="bi bi-check-circle"></i> <strong>${b.title}</strong> by ${b.author}</div>`);
    $('#availableBooksList').html('');
    $('#searchBook').val('');
    updateConfirm();
}

function deselectBook() {
    $('#bookId').val('');
    $('#bookResult').html('');
    $('#borrowBtn').prop('disabled', true);
    $('#confirmInfo').html('<span class="text-muted">Select a borrower and a book to continue.</span>');
    loadAvailableBooks();
}

function updateConfirm() {
    const bid = $('#borrowerId').val();
    const bookId = $('#bookId').val();
    if (bid && bookId) {
        $('#borrowBtn').prop('disabled', false);
        $('#confirmInfo').html('<span class="text-success">Ready to process borrowing.</span>');
    }
}

// Auto-init based on which page elements exist
$(function() {
    if ($('#booksList').length > 0 && $('#searchBook').length === 0) {
        initReturns();
    } else if ($('#borrowBtn').length > 0) {
        initBorrowing();
    }
});

// === Returns Module (AL) ===
let alReturnsTimeout;

function initALReturns() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(alReturnsTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            alReturnsTimeout = setTimeout(() => processALBarcode(val), 400);
        }
    });

    $('#searchBorrower').on('input', function() {
        clearTimeout(alReturnsTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            alReturnsTimeout = setTimeout(() => alReturnsSearchBorrowerByName(val), 400);
        } else {
            $('#borrowerResult').html('');
        }
    });
}

function alReturnsSearchBorrowerByName(term) {
    $.getJSON('/AssistantLibrarian/Returns/SearchBorrower', { term: term }, function(data) {
        if (data.length === 0) {
            $('#borrowerResult').html('<div class="text-muted">No matches.</div>');
        } else {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="alReturnsSelectBorrower('${b.barcode}')">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        }
    });
}

function alReturnsSelectBorrower(barcode) {
    $('#borrowerResult').html('');
    $('#searchBorrower').val('');
    $('#barcodeInput').val(barcode);
    processALBarcode(barcode);
}

function processALBarcode(barcode) {
    $.post('/AssistantLibrarian/Returns/Process', { borrowerBarcode: barcode }, function(res) {
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

            let html = '<div class="list-group">';
            res.books.forEach(b => {
                const showExtend = canExtend(b.wasExtended, b.dueDate);
                html += `<div class="list-group-item d-flex justify-content-between align-items-center">
                    <div><strong>${b.bookTitle}</strong><br/><small class="text-muted">Due: ${new Date(b.dueDate).toLocaleDateString()}</small></div>
                    <div class="d-flex gap-1">
                        ${showExtend ? `<button class="btn btn-warning btn-sm" onclick="alExtendBorrow(${b.id})"><i class="bi bi-clock"></i> Extend</button>` : ''}
                        <button class="btn btn-success" onclick="alReturnBook(${b.id})">Return</button>
                    </div>
                </div>`;
            });
            html += '</div>';
            $('#booksList').html(html);
        } else {
            $('#borrowerInfo').html(`<span class="text-danger">${res.message}</span>`);
            $('#booksList').html('');
        }
    });
}

function alExtendBorrow(recordId) {
    if (!confirm('Extend this borrowing by 2 days?')) return;

    $.post('/AssistantLibrarian/Returns/Extend', { recordId: recordId }, function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            resetALReturns();
        } else { showToast(res.message, 'danger'); }
    });
}

function alReturnBook(id) {
    $.post('/AssistantLibrarian/Returns/ReturnBook', { recordId: id }, function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            resetALReturns();
        } else { showToast(res.message, 'danger'); }
    });
}

function resetALReturns() {
    $('#barcodeInput').val('').focus();
    $('#searchBorrower').val('');
    $('#borrowerResult').html('');
    $('#booksList').html('');
    $('#borrowerInfo').html('<span class="text-muted small">Awaiting scan...</span>');
}

// === Borrowing Module (AL) ===
let alBorrowerTimeout, alBookTimeout;

function initALBorrowing() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(alBorrowerTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            alBorrowerTimeout = setTimeout(() => alLookupBorrower(val), 400);
        }
    });

    $('#searchBorrower').on('input', function() {
        clearTimeout(alBorrowerTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            alBorrowerTimeout = setTimeout(() => alSearchBorrower(val), 400);
        }
    });

    $('#searchBook').on('input', function() {
        clearTimeout(alBookTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            alBookTimeout = setTimeout(() => alSearchBooks(val), 400);
        } else {
            alLoadAvailableBooks();
        }
    });

    $('#borrowBtn').click(function() {
        const borrowerBarcode = $('#borrowerBarcode').val();
        const bookId = $('#bookId').val();
        if (!borrowerBarcode || !bookId) return;

        $(this).prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Processing...');

        $.post('/AssistantLibrarian/Borrowing/Process', { borrowerBarcode: borrowerBarcode, bookId: bookId }, function(res) {
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

function alLookupBorrower(barcode) {
    $.getJSON('/AssistantLibrarian/Borrowing/SearchBorrower', { term: barcode }, function(data) {
        if (data.length === 1 && data[0].barcode === barcode) {
            alSelectBorrower(data[0]);
        } else if (data.length > 0) {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="alSelectBorrower(${JSON.stringify(b).replace(/"/g, "'")})">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        } else {
            $('#borrowerResult').html('<div class="text-danger">Borrower not found.</div>');
        }
    });
}

function alSearchBorrower(term) {
    $.getJSON('/AssistantLibrarian/Borrowing/SearchBorrower', { term: term }, function(data) {
        if (data.length === 0) {
            $('#borrowerResult').html('<div class="text-muted">No matches.</div>');
        } else {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="alSelectBorrower(${JSON.stringify(b).replace(/"/g, "'")})">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        }
    });
}

function alSelectBorrower(b) {
    $('#borrowerId').val(b.id);
    $('#borrowerBarcode').val(b.barcode);
    $('#borrowerResult').html(`<div class="alert alert-success py-2 mb-0">
        <i class="bi bi-check-circle"></i> <strong>${b.name}</strong> (${b.barcode})</div>`);
    $('#bookLockedNotice').hide();
    $('#bookSearchSection').show();
    alUnlockAvailableBooks();
}

function alUnlockAvailableBooks() {
    $('#booksLockedOverlay').hide();
    $('#availableBooksList').show();
    alLoadAvailableBooks();
}

function alLoadAvailableBooks() {
    $.getJSON('/AssistantLibrarian/Borrowing/GetAvailableBooks', function(data) {
        $('#bookCount').text(data.length + ' books');
        if (data.length === 0) {
            $('#availableBooksList').html('<div class="text-center text-muted py-4">No available books.</div>');
        } else {
            let html = '<ul class="list-group list-group-flush">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action py-2" onclick="alSelectBook(${JSON.stringify(b).replace(/"/g, "'")})">
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

function alSearchBooks(term) {
    $.getJSON('/AssistantLibrarian/Borrowing/SearchBook', { term: term }, function(data) {
        if (data.length === 0) {
            $('#availableBooksList').html('<div class="text-center text-muted py-4">No available books found.</div>');
        } else {
            let html = '<ul class="list-group list-group-flush">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action py-2" onclick="alSelectBook(${JSON.stringify(b).replace(/"/g, "'")})">
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

function alSelectBook(b) {
    if (!$('#borrowerId').val()) return;
    $('#bookId').val(b.id);
    $('#bookResult').html(`<div class="alert alert-success py-2 mb-0" style="cursor:pointer;" onclick="alDeselectBook()" title="Click to choose a different book">
        <i class="bi bi-check-circle"></i> <strong>${b.title}</strong> by ${b.author}</div>`);
    $('#availableBooksList').html('');
    $('#searchBook').val('');
    alUpdateConfirm();
}

function alDeselectBook() {
    $('#bookId').val('');
    $('#bookResult').html('');
    $('#borrowBtn').prop('disabled', true);
    $('#confirmInfo').html('<span class="text-muted">Select a borrower and a book to continue.</span>');
    alLoadAvailableBooks();
}

function alUpdateConfirm() {
    const bid = $('#borrowerId').val();
    const bookId = $('#bookId').val();
    if (bid && bookId) {
        $('#borrowBtn').prop('disabled', false);
        $('#confirmInfo').html('<span class="text-success">Ready to process borrowing.</span>');
    }
}

// === Fines Module (AL) ===
let alFinesTimeout;

function initALFines() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(alFinesTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            alFinesTimeout = setTimeout(() => alFinesLookupBorrower(val), 400);
        }
    });

    $('#searchBorrower').on('input', function() {
        clearTimeout(alFinesTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            alFinesTimeout = setTimeout(() => alFinesSearchBorrowerByName(val), 400);
        } else {
            $('#borrowerResult').html('');
        }
    });
}

function alFinesLookupBorrower(barcode) {
    $.post('/AssistantLibrarian/Fines/Search', { borrowerBarcode: barcode }, function(res) {
        if (res.success) {
            $('#borrowerInfo').html(`<span class="text-success"><i class="bi bi-person-check"></i> <strong>${res.borrowerName}</strong></span>`);
            if (res.fines.length === 0) {
                $('#finesList').html('<div class="alert alert-success">No unpaid fines.</div>');
                return;
            }
            let html = '<div class="list-group">';
            let total = 0;
            res.fines.forEach(f => {
                total += f.amount;
                html += `<div class="list-group-item d-flex justify-content-between align-items-center">
                    <div><strong>${f.bookTitle}</strong><br/><small>Fine: PHP ${f.amount.toFixed(2)}</small></div>
                    <button class="btn btn-primary" onclick="alPayFine(${f.id})">Collect PHP ${f.amount.toFixed(2)}</button>
                </div>`;
            });
            html += `<div class="list-group-item fw-bold">Total: PHP ${total.toFixed(2)}</div>`;
            html += '</div>';
            $('#finesList').html(html);
        } else {
            $('#borrowerInfo').html(`<span class="text-danger">${res.message}</span>`);
            $('#finesList').html('');
        }
    });
}

function alFinesSearchBorrowerByName(term) {
    $.getJSON('/AssistantLibrarian/Fines/SearchBorrower', { term: term }, function(data) {
        if (data.length === 0) {
            $('#borrowerResult').html('<div class="text-muted">No matches.</div>');
        } else {
            let html = '<ul class="list-group">';
            data.forEach(b => {
                html += `<li class="list-group-item list-group-item-action" onclick="alFinesSelectBorrower('${b.barcode}')">
                    <strong>${b.name}</strong> <code>${b.barcode}</code> ${b.grade ? '-' + b.grade : ''}</li>`;
            });
            html += '</ul>';
            $('#borrowerResult').html(html);
        }
    });
}

function alFinesSelectBorrower(barcode) {
    $('#borrowerResult').html('');
    $('#searchBorrower').val('');
    $('#barcodeInput').val(barcode);
    alFinesLookupBorrower(barcode);
}

function alPayFine(id) {
    if (!confirm('Collect payment for this fine?')) return;
    $.post('/AssistantLibrarian/Fines/Pay', { fineId: id }, function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            if (res.receipt) {
                var r = res.receipt;
                document.getElementById('receiptBody').innerHTML =
                    '<div class="text-center mb-3"><i class="bi bi-check-circle-fill text-success fs-1"></i></div>' +
                    '<table class="table table-sm table-borderless">' +
                    '<tr><td class="text-muted">Borrower</td><td class="text-end fw-semibold">' + r.borrower + '</td></tr>' +
                    '<tr><td class="text-muted">Book</td><td class="text-end">' + r.book + '</td></tr>' +
                    '<tr><td class="text-muted">Barcode</td><td class="text-end"><code>' + r.barcode + '</code></td></tr>' +
                    '<tr><td class="text-muted">Amount</td><td class="text-end fw-bold text-success">PHP ' + r.amount + '</td></tr>' +
                    '<tr><td class="text-muted">Paid At</td><td class="text-end">' + r.paidAt + '</td></tr>' +
                    '</table>';
                new bootstrap.Modal(document.getElementById('receiptModal')).show();
            }
            $('#barcodeInput').val('').focus();
            $('#searchBorrower').val('');
            $('#borrowerResult').html('');
            $('#finesList').html('');
            $('#borrowerInfo').html('<span class="text-muted small">Awaiting scan...</span>');
        } else { showToast(res.message, 'danger'); }
    });
}

// Auto-init based on which page elements exist
$(function() {
    if ($('#finesList').length > 0) {
        initALFines();
    } else if ($('#booksList').length > 0 && $('#searchBook').length === 0) {
        initALReturns();
    } else if ($('#borrowBtn').length > 0) {
        initALBorrowing();
    }
});
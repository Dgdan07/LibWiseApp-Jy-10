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
    $('#booksList').html('');
    $('#borrowerInfo').html('<span class="text-muted small">Awaiting scan...</span>');
}

// === Borrowing Module (AL) ===
let alBorrowTimeout, alBookTimeout;

function initALBorrowing() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(alBorrowTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            alBorrowTimeout = setTimeout(() => {
                $.getJSON('/AssistantLibrarian/Borrowing/SearchBorrower', { term: val }, function(data) {
                    if (data.length === 1 && data[0].barcode === val) {
                        alSelectBorrower(data[0]);
                    } else if (data.length > 0) {
                        let html = '<ul class="list-group">';
                        data.forEach(b => { html += `<li class="list-group-item list-group-item-action" onclick="alSelectBorrower(${JSON.stringify(b).replace(/"/g, "'")})"><strong>${b.name}</strong> <code>${b.barcode}</code></li>`; });
                        $('#borrowerResult').html(html + '</ul>');
                    } else {
                        $('#borrowerResult').html('<div class="text-danger">Not found.</div>');
                    }
                });
            }, 400);
        }
    });

    $('#searchBook').on('input', function() {
        clearTimeout(alBookTimeout);
        const val = this.value.trim();
        if (val.length > 2) {
            alBookTimeout = setTimeout(() => {
                $.getJSON('/AssistantLibrarian/Borrowing/SearchBook', { term: val }, function(data) {
                    if (data.length === 0) { $('#bookResult').html('<div class="text-muted">No matches.</div>'); return; }
                    let html = '<ul class="list-group">';
                    data.forEach(b => { html += `<li class="list-group-item list-group-item-action" onclick="alSelectBook(${JSON.stringify(b).replace(/"/g, "'")})"><strong>${b.title}</strong> by ${b.author}<br/><small>Available: ${b.availableCopies}</small></li>`; });
                    $('#bookResult').html(html + '</ul>');
                });
            }, 400);
        }
    });

    $('#borrowBtn').click(function() {
        const bc = $('#borrowerBarcode').val(), bk = $('#bookId').val();
        if (!bc || !bk) return;
        $(this).prop('disabled', true).html('Processing...');
        $.post('/AssistantLibrarian/Borrowing/Process', { borrowerBarcode: bc, bookId: bk }, function(res) {
            if (res.success) { showToast(res.message, 'success'); location.reload(); }
            else { showToast('Error: ' + res.message, 'danger'); $('#borrowBtn').prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Confirm Borrowing'); }
        }).fail(function() { showToast('Failed.', 'danger'); location.reload(); });
    });
}

function alSelectBorrower(b) {
    $('#borrowerId').val(b.id);
    $('#borrowerBarcode').val(b.barcode);
    $('#borrowerResult').html(`<div class="alert alert-success py-2 mb-0"><i class="bi bi-check-circle"></i> <strong>${b.name}</strong></div>`);
    $('#searchBook').prop('disabled', false).focus();
}

function alSelectBook(b) {
    $('#bookId').val(b.id);
    $('#bookResult').html(`<div class="alert alert-success py-2 mb-0"><i class="bi bi-check-circle"></i> <strong>${b.title}</strong></div>`);
    $('#borrowBtn').prop('disabled', false);
}

// === Fines Module (AL) ===
let alFinesTimeout;

function initALFines() {
    $('#barcodeInput').on('input', function() {
        clearTimeout(alFinesTimeout);
        const val = this.value.trim();
        if (val.length > 3) {
            alFinesTimeout = setTimeout(() => {
                $.post('/AssistantLibrarian/Fines/Search', { borrowerBarcode: val }, function(res) {
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
            }, 400);
        }
    });
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
function showFetchSpinner() {
    let el = document.getElementById('ajaxSpinner');
    if (el) el.style.display = 'inline-block';
}

function hideFetchSpinner() {
    let el = document.getElementById('ajaxSpinner');
    if (el) el.style.display = 'none';
}

function renderModalPagination(containerId, page, totalPages, loader) {
    let container = document.getElementById(containerId);
    if (!container) return;
    totalPages = Math.max(totalPages, 1);
    container.innerHTML =
        '<ul class="pagination pagination-sm justify-content-end mb-0">' +
        '<li class="page-item ' + (page <= 1 ? 'disabled' : '') + '">' +
        '<a href="#" class="page-link" data-page="' + (page - 1) + '"><i class="bi bi-chevron-left"></i></a>' +
        '</li>' +
        '<li class="page-item disabled"><span class="page-link">Page ' + page + ' of ' + totalPages + '</span></li>' +
        '<li class="page-item ' + (page >= totalPages ? 'disabled' : '') + '">' +
        '<a href="#" class="page-link" data-page="' + (page + 1) + '"><i class="bi bi-chevron-right"></i></a>' +
        '</li>' +
        '</ul>';
    container.querySelectorAll('a.page-link').forEach(function (a) {
        a.addEventListener('click', function (e) {
            e.preventDefault();
            if (this.closest('.page-item').classList.contains('disabled')) return;
            loader(parseInt(this.dataset.page));
        });
    });
}

function loadUnpaidFines(page, onDone) {
    showFetchSpinner();
    fetch('/Admin/Reports/GetUnpaidFines?page=' + page)
        .then(r => r.json())
        .then(res => {
            hideFetchSpinner();
            let tbody = document.getElementById('unpaidFinesBody');
            if (res.items.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No unpaid fines.</td></tr>';
            } else {
                tbody.innerHTML = res.items.map(f => '<tr><td>' + f.borrower + '</td><td>' + f.book + '</td><td>PHP ' + f.amount + '</td><td>' + f.dueDate + '</td><td>' + f.calculatedAt + '</td></tr>').join('');
            }
            renderModalPagination('unpaidFinesPagination', res.page, res.totalPages, p => loadUnpaidFines(p));
            if (onDone) onDone();
        });
}

function showUnpaidFines() {
    loadUnpaidFines(1, () => {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('unpaidFinesModal')).show();
    });
}

function loadActiveBorrowings(page, onDone) {
    showFetchSpinner();
    fetch('/Admin/Reports/GetActiveBorrowings?page=' + page)
        .then(r => r.json())
        .then(res => {
            hideFetchSpinner();
            let tbody = document.getElementById('activeBorrowingsBody');
            if (res.items.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No active borrowings.</td></tr>';
            } else {
                tbody.innerHTML = res.items.map(r => '<tr><td>' + r.borrower + '</td><td>' + r.book + '</td><td>' + r.borrowedAt + '</td><td>' + r.dueDate + '</td></tr>').join('');
            }
            renderModalPagination('activeBorrowingsPagination', res.page, res.totalPages, p => loadActiveBorrowings(p));
            if (onDone) onDone();
        });
}

function showActiveBorrowings() {
    loadActiveBorrowings(1, () => {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('activeBorrowingsModal')).show();
    });
}

function loadOverdueBorrowings(page, onDone) {
    showFetchSpinner();
    fetch('/Admin/Reports/GetOverdueBorrowings?page=' + page)
        .then(r => r.json())
        .then(res => {
            hideFetchSpinner();
            let tbody = document.getElementById('overdueBorrowingsBody');
            if (res.items.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No overdue borrowings.</td></tr>';
            } else {
                tbody.innerHTML = res.items.map(r => '<tr><td>' + r.borrower + '</td><td>' + r.book + '</td><td>' + r.borrowedAt + '</td><td>' + r.dueDate + '</td><td>' + r.daysOverdue + '</td></tr>').join('');
            }
            renderModalPagination('overdueBorrowingsPagination', res.page, res.totalPages, p => loadOverdueBorrowings(p));
            if (onDone) onDone();
        });
}

function showOverdueBorrowings() {
    loadOverdueBorrowings(1, () => {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('overdueBorrowingsModal')).show();
    });
}

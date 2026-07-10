function showFetchSpinner() {
    let el = document.getElementById('ajaxSpinner');
    if (el) el.style.display = 'inline-block';
}

function hideFetchSpinner() {
    let el = document.getElementById('ajaxSpinner');
    if (el) el.style.display = 'none';
}

function showUnpaidFines() {
    showFetchSpinner();
    fetch('/Admin/Reports/GetUnpaidFines')
        .then(r => r.json())
        .then(data => {
            hideFetchSpinner();
            let tbody = document.getElementById('unpaidFinesBody');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No unpaid fines.</td></tr>';
            } else {
                tbody.innerHTML = data.map(f => '<tr><td>' + f.borrower + '</td><td>' + f.book + '</td><td>PHP ' + f.amount + '</td><td>' + f.dueDate + '</td><td>' + f.calculatedAt + '</td></tr>').join('');
            }
            new bootstrap.Modal(document.getElementById('unpaidFinesModal')).show();
        });
}

function showActiveBorrowings() {
    showFetchSpinner();
    fetch('/Admin/Reports/GetActiveBorrowings')
        .then(r => r.json())
        .then(data => {
            hideFetchSpinner();
            let tbody = document.getElementById('activeBorrowingsBody');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No active borrowings.</td></tr>';
            } else {
                tbody.innerHTML = data.map(r => '<tr><td>' + r.borrower + '</td><td>' + r.book + '</td><td>' + r.borrowedAt + '</td><td>' + r.dueDate + '</td></tr>').join('');
            }
            new bootstrap.Modal(document.getElementById('activeBorrowingsModal')).show();
        });
}

function showOverdueBorrowings() {
    showFetchSpinner();
    fetch('/Admin/Reports/GetOverdueBorrowings')
        .then(r => r.json())
        .then(data => {
            hideFetchSpinner();
            let tbody = document.getElementById('overdueBorrowingsBody');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No overdue borrowings.</td></tr>';
            } else {
                tbody.innerHTML = data.map(r => '<tr><td>' + r.borrower + '</td><td>' + r.book + '</td><td>' + r.borrowedAt + '</td><td>' + r.dueDate + '</td><td>' + r.daysOverdue + '</td></tr>').join('');
            }
            new bootstrap.Modal(document.getElementById('overdueBorrowingsModal')).show();
        });
}
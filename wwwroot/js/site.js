// Global AJAX spinner
$(function () {
    $('body').append('<div id="ajaxSpinner" class="spinner-border text-primary" role="status" style="position:fixed;top:1rem;right:1rem;z-index:9999;display:none"><span class="visually-hidden">Loading...</span></div>');
});

$(document).ajaxStart(function () { $('#ajaxSpinner').show(); });
$(document).ajaxStop(function () { $('#ajaxSpinner').hide(); });

function printReceipt() {
    var body = document.getElementById('receiptBody').innerHTML;
    var win = window.open('', '', 'width=400,height=600');
    win.document.write('<html><head><title>Receipt</title><link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css"></head><body><div class="container p-4">' + body + '</div></body></html>');
    win.document.close();
    win.print();
}

function showToast(message, type) {
    type = type || 'info';
    var icons = { success: 'bi-check-circle-fill', danger: 'bi-x-circle-fill', warning: 'bi-exclamation-circle-fill', info: 'bi-info-circle-fill' };
    var icon = icons[type] || icons.info;
    var toastHtml = '<div class="toast align-items-center text-bg-' + type + ' border-0" role="alert" aria-live="assertive" aria-atomic="true">' +
        '<div class="d-flex"><div class="toast-body"><i class="bi ' + icon + ' me-2"></i>' + message + '</div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div></div>';
    var $toast = $(toastHtml).appendTo('#toastContainer');
    new bootstrap.Toast($toast[0], { autohide: true, delay: 4000 }).show();
    $toast.on('hidden.bs.toast', function () { $(this).remove(); });
}

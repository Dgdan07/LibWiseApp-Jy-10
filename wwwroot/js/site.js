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

// AJAX pagination: clicking a page-link inside a [data-ajax-page] container
// swaps only that container's HTML instead of doing a full page navigation.
(function () {
    function swapContainer(html, id) {
        var doc = new DOMParser().parseFromString(html, 'text/html');
        var next = doc.getElementById(id);
        var current = document.getElementById(id);
        if (next && current) {
            current.replaceWith(next);
        }
    }

    function loadInto(url, id) {
        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.text(); })
            .then(function (html) { swapContainer(html, id); })
            .catch(function () { window.location.href = url; });
    }

    document.addEventListener('click', function (e) {
        var link = e.target.closest('[data-ajax-page] a.page-link');
        if (!link) return;
        var item = link.closest('.page-item');
        if (item && item.classList.contains('disabled')) return;
        var container = link.closest('[data-ajax-page]');
        if (!container || !container.id) return;

        e.preventDefault();
        var url = link.getAttribute('href');
        history.pushState({ ajaxPage: true }, '', url);
        loadInto(url, container.id);
    });

    window.addEventListener('popstate', function () {
        document.querySelectorAll('[data-ajax-page]').forEach(function (container) {
            if (container.id) loadInto(location.href, container.id);
        });
    });
})();

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

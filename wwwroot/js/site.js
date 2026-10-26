// RestauranteMVC — comportamiento común de la interfaz
(function () {
    'use strict';

    // Menú lateral en móvil
    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-nav-toggle]')) {
            document.body.classList.toggle('nav-open');
        } else if (e.target.closest('.scrim') || (e.target.closest('.sidebar .nav-x'))) {
            document.body.classList.remove('nav-open');
        }
        // Cerrar avisos
        var cerrar = e.target.closest('[data-dismiss-flash]');
        if (cerrar) { cerrar.closest('.flash').remove(); }
    });

    // Confirmación antes de acciones delicadas: <form data-confirm="¿Seguro?">
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) { return; }

        var mensaje = form.getAttribute('data-confirm');
        if (mensaje && !window.confirm(mensaje)) {
            e.preventDefault();
            return;
        }

        // Evita envíos duplicados (doble clic crea dos pedidos / dos líneas)
        if (e.defaultPrevented) { return; }
        if (form.dataset.enviado === '1') { e.preventDefault(); return; }
        form.dataset.enviado = '1';
        form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(function (b) {
            b.classList.add('disabled');
            b.setAttribute('aria-disabled', 'true');
        });
    });

    // Al volver con el botón "atrás" el formulario debe poder enviarse otra vez
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('form[data-enviado]').forEach(function (f) {
            delete f.dataset.enviado;
            f.querySelectorAll('.disabled[type="submit"]').forEach(function (b) {
                b.classList.remove('disabled');
                b.removeAttribute('aria-disabled');
            });
        });
    });

    // Los avisos de éxito desaparecen solos
    window.setTimeout(function () {
        document.querySelectorAll('.flash.ok').forEach(function (el) { el.remove(); });
    }, 6000);
})();

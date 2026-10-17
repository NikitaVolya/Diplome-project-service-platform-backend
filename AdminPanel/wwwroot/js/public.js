(() => {
    const burger = document.querySelector('.v-burger');
    const navigation = document.getElementById('public-navigation');
    const compact = window.matchMedia('(max-width: 1280px)');

    const closeMenu = (restoreFocus = false) => {
        navigation?.classList.remove('is-open');
        burger?.setAttribute('aria-expanded', 'false');
        burger?.setAttribute('aria-label', 'Відкрити меню');
        if (restoreFocus) burger?.focus();
    };

    burger?.addEventListener('click', () => {
        const expanded = burger.getAttribute('aria-expanded') !== 'true';
        navigation?.classList.toggle('is-open', expanded);
        burger.setAttribute('aria-expanded', String(expanded));
        burger.setAttribute('aria-label', expanded ? 'Закрити меню' : 'Відкрити меню');
    });
    navigation?.addEventListener('click', event => {
        if (event.target.closest('a')) closeMenu();
    });
    document.addEventListener('click', event => {
        if (!navigation?.contains(event.target) && !burger?.contains(event.target)) closeMenu();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && burger?.getAttribute('aria-expanded') === 'true') closeMenu(true);
    });
    compact.addEventListener('change', () => closeMenu());

    const dialog = document.getElementById('catalog-info');
    document.querySelectorAll('[data-catalog-info]').forEach(button => {
        button.addEventListener('click', () => {
            if (dialog && !dialog.open) dialog.showModal();
        });
    });
    dialog?.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const box = dialog.getBoundingClientRect();
        if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
    });
})();

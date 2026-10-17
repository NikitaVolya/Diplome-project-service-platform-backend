(() => {
    const show = document.getElementById('show-executors');
    show?.addEventListener('click', () => {
        const expanded = show.getAttribute('aria-expanded') !== 'true';
        document.querySelectorAll('.catalog-executor').forEach((card, i) => { card.hidden = !expanded && i >= 4; });
        show.setAttribute('aria-expanded', String(expanded));
        show.textContent = expanded ? 'Показати менше ↑' : 'Переглянути всіх →';
    });
})();

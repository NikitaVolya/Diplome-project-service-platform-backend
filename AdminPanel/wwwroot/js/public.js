(() => {
    const burger = document.querySelector('.v-burger');
    const navigation = document.getElementById('public-navigation');
    const compact = window.matchMedia('(max-width: 1280px)');
    // Підписи кнопки меню приходять із розмітки вже перекладеними.
    const label = expanded => expanded
        ? (burger?.dataset.labelClose || 'Закрити меню')
        : (burger?.dataset.labelOpen || 'Відкрити меню');

    const closeMenu = (restoreFocus = false) => {
        navigation?.classList.remove('is-open');
        burger?.setAttribute('aria-expanded', 'false');
        burger?.setAttribute('aria-label', label(false));
        if (restoreFocus) burger?.focus();
    };

    burger?.addEventListener('click', () => {
        const expanded = burger.getAttribute('aria-expanded') !== 'true';
        navigation?.classList.toggle('is-open', expanded);
        burger.setAttribute('aria-expanded', String(expanded));
        burger.setAttribute('aria-label', label(expanded));
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

    // Перемикач мови: закривається кліком поза ним, Escape або по рядку з поточною мовою.
    const language = document.querySelector('.v-lang');
    const closeLanguage = () => { if (language) language.open = false; };
    language?.querySelector('[data-lang-close]')?.addEventListener('click', () => {
        closeLanguage();
        language.querySelector('summary')?.focus();
    });
    document.addEventListener('click', event => {
        if (language?.open && !language.contains(event.target)) closeLanguage();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && language?.open) {
            closeLanguage();
            language.querySelector('summary')?.focus();
        }
    });

    // Вікно «Оберіть дію» замість прямого переходу на реєстрацію.
    const roleChoice = document.getElementById('role-choice');
    if (roleChoice?.showModal) {
        document.querySelectorAll('[data-role-choice]').forEach(link => {
            link.addEventListener('click', event => {
                event.preventDefault();
                closeMenu();
                roleChoice.showModal();
            });
        });
        // Клік по затемненню навколо вікна закриває його.
        roleChoice.addEventListener('click', event => {
            if (event.target !== roleChoice) return;
            const box = roleChoice.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) roleChoice.close();
        });
    }

    // Фото профілю в кабінеті: перевіряємо розмір ще до відправки, показуємо
    // прев'ю й одразу надсилаємо окрему форму avatar-form.
    const avatarForm = document.getElementById('avatar-form');
    document.querySelectorAll('[data-avatar-input]').forEach(input => {
        input.addEventListener('change', () => {
            const file = input.files?.[0];
            if (!file || !avatarForm) return;
            if (file.size > 2 * 1024 * 1024) {
                alert(input.dataset.tooBig || 'Фото завелике — максимум 2 Мб.');
                input.value = '';
                return;
            }
            const preview = input.closest('.cab-set__photo, .exec-set__photo')?.querySelector('img');
            if (preview) preview.src = URL.createObjectURL(file);
            input.closest('label')?.classList.add('is-busy');
            avatarForm.requestSubmit ? avatarForm.requestSubmit() : avatarForm.submit();
        });
    });

    // Перемикач «Тема інтерфейсу»: показуємо тему одразу, а зберігається вона кнопкою «Зберегти зміни».
    const applyTheme = theme => {
        const root = document.documentElement;
        if (theme === 'dark') root.dataset.theme = 'dark'; else delete root.dataset.theme;
        document.querySelector('meta[name="color-scheme"]')?.setAttribute('content', theme === 'dark' ? 'dark' : 'light');
        document.querySelectorAll('img[data-logo-light]').forEach(img => {
            img.src = theme === 'dark' ? img.dataset.logoDark : img.dataset.logoLight;
        });
    };
    document.querySelectorAll('input[name="Theme"]').forEach(input => {
        input.addEventListener('change', () => { if (input.checked) applyTheme(input.value); });
    });

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

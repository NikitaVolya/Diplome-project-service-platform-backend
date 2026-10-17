(() => {
    'use strict';

    // ── Око «показати пароль» ───────────────────────────────────────────────
    document.querySelectorAll('[data-auth-password]').forEach(box => {
        const input = box.querySelector('input');
        const eye = box.querySelector('.auth-eye');
        if (!input || !eye) return;

        eye.addEventListener('click', () => {
            const shown = input.type === 'text';
            input.type = shown ? 'password' : 'text';
            eye.setAttribute('aria-label', shown ? 'Показати пароль' : 'Приховати пароль');
            input.focus();
        });
    });

    // ── Іконка й підзаголовок міняються разом із вибраною роллю ─────────────
    const roleInputs = document.querySelectorAll('[data-role-pick]');
    if (roleInputs.length) {
        const swap = () => {
            const picked = document.querySelector('[data-role-pick]:checked');
            const role = picked ? picked.value : 'Customer';
            document.querySelectorAll('[data-role-icon], [data-role-lead]').forEach(node => {
                const own = node.dataset.roleIcon || node.dataset.roleLead;
                node.classList.toggle('d-none', own !== role);
            });
        };
        roleInputs.forEach(input => input.addEventListener('change', swap));
        swap();
    }

    // ── Шість клітинок коду ────────────────────────────────────────────────
    const code = document.querySelector('[data-auth-code]');
    if (code) {
        const cells = [...code.querySelectorAll('input')];
        const hidden = code.parentElement.querySelector('input[type="hidden"]');

        const collect = () => {
            if (hidden) hidden.value = cells.map(c => c.value).join('');
        };

        cells.forEach((cell, i) => {
            cell.addEventListener('input', () => {
                cell.value = cell.value.replace(/\D/g, '').slice(-1);
                if (cell.value && i < cells.length - 1) cells[i + 1].focus();
                collect();
            });

            cell.addEventListener('keydown', e => {
                if (e.key === 'Backspace' && !cell.value && i > 0) {
                    cells[i - 1].focus();
                    cells[i - 1].value = '';
                    collect();
                    e.preventDefault();
                }
                if (e.key === 'ArrowLeft' && i > 0) cells[i - 1].focus();
                if (e.key === 'ArrowRight' && i < cells.length - 1) cells[i + 1].focus();
            });

            cell.addEventListener('paste', e => {
                const text = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '');
                if (!text) return;
                e.preventDefault();
                cells.forEach((c, j) => { c.value = text[j] || ''; });
                collect();
                cells[Math.min(text.length, cells.length - 1)].focus();
            });
        });

        cells[0]?.focus();
    }

    // ── Зворотний відлік для «надіслати ще раз» ─────────────────────────────
    const resend = document.querySelector('[data-auth-resend]');
    if (resend) {
        let left = parseInt(resend.dataset.authResend, 10) || 0;
        const label = resend.textContent;

        const tick = () => {
            if (left <= 0) {
                resend.disabled = false;
                resend.textContent = label;
                return;
            }
            const mm = String(Math.floor(left / 60)).padStart(2, '0');
            const ss = String(left % 60).padStart(2, '0');
            resend.disabled = true;
            resend.textContent = `${label} (${mm}:${ss})`;
            left -= 1;
            setTimeout(tick, 1000);
        };

        tick();
    }

    // ── Галочки під полем нового пароля ────────────────────────────────────
    const strength = document.querySelector('[data-auth-strength]');
    const rules = document.querySelector('[data-auth-rules]');
    if (strength && rules) {
        const checks = {
            length: v => v.length >= 8,
            case: v => /[a-zа-яґєії]/.test(v) && /[A-ZА-ЯҐЄІЇ]/.test(v),
            digit: v => /\d/.test(v) || /[^\w\s]/.test(v)
        };

        const update = () => {
            const value = strength.value;
            rules.querySelectorAll('[data-rule]').forEach(li => {
                li.classList.toggle('is-met', checks[li.dataset.rule](value));
            });
        };

        strength.addEventListener('input', update);
        update();
    }


    // ── Назва компанії зʼявляється тільки для «Компанія/ФОП» ───────────────
    const companyField = document.querySelector('[data-company-field]');
    if (companyField) {
        const picks = document.querySelectorAll('[data-company]');
        const sync = () => {
            const on = [...picks].some(p => p.checked && p.dataset.company === 'true');
            companyField.hidden = !on;
        };
        picks.forEach(p => p.addEventListener('change', sync));
        sync();
    }

    // ── «Назад» веде в історію, якщо є куди ────────────────────────────────
    document.querySelectorAll('[data-auth-back]').forEach(link => {
        link.addEventListener('click', e => {
            if (document.referrer && history.length > 1) {
                e.preventDefault();
                history.back();
            }
        });
    });
})();

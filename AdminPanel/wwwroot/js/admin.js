/* Спільна поведінка всіх сторінок адмінки: згортання бічного меню, підтвердження дій
   і налаштування Chart.js за замовчуванням для дашборда та статистики. */
(function () {
    'use strict';

    // ------------------------------------------------------------- бічне меню

    function initSidebar() {
        var toggle = document.querySelector('[data-sidebar-toggle]');
        var sidebar = document.querySelector('.sidebar');
        if (!toggle || !sidebar) { return; }

        var backdrop = null;

        function close() {
            sidebar.classList.remove('is-open');
            if (backdrop) { backdrop.remove(); backdrop = null; }
        }

        toggle.addEventListener('click', function () {
            var isOpen = sidebar.classList.toggle('is-open');

            if (isOpen) {
                backdrop = document.createElement('div');
                backdrop.className = 'sidebar-backdrop';
                backdrop.addEventListener('click', close);
                document.body.appendChild(backdrop);
            } else {
                close();
            }
        });

        window.addEventListener('resize', function () {
            if (window.innerWidth >= 992) { close(); }
        });
    }

    // ---------------------------------------------------------- підтвердження

    /* Будь-яка форма з атрибутом data-confirm перепитує перед відправленням. Так небезпечні дії
   захищені від випадкового кліку без окремого модального вікна на кожну кнопку. */
    function initConfirmations() {
        document.addEventListener('submit', function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement)) { return; }

            var message = form.getAttribute('data-confirm');
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    }

    // ---------------------------------------------------- помічники фільтрів

    function initFilters() {
        // Списки й поля дат усередині панелі фільтрів відправляють форму одразу.
        document.querySelectorAll('.filter-bar [data-auto-submit]').forEach(function (control) {
            control.addEventListener('change', function () {
                var form = control.closest('form');
                if (form) { form.submit(); }
            });
        });

        document.querySelectorAll('[data-reset-filters]').forEach(function (button) {
            button.addEventListener('click', function () {
                window.location = window.location.pathname;
            });
        });
    }

    // --------------------------------------------------------------- графіки

    var palette = ['#4f46e5', '#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#14b8a6', '#f43f5e'];

    function applyChartDefaults() {
        if (typeof window.Chart === 'undefined') { return; }

        window.Chart.defaults.font.family =
            '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
        window.Chart.defaults.font.size = 12;
        window.Chart.defaults.color = '#64748b';
        window.Chart.defaults.plugins.legend.labels.usePointStyle = true;
        window.Chart.defaults.plugins.legend.labels.boxWidth = 8;
        window.Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(15, 23, 42, .92)';
        window.Chart.defaults.plugins.tooltip.padding = 10;
        window.Chart.defaults.plugins.tooltip.cornerRadius = 8;
        window.Chart.defaults.maintainAspectRatio = false;
    }

    function gridScales(options) {
        options = options || {};
        return {
            x: {
                grid: { display: false },
                ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: options.maxTicks || 10 }
            },
            y: {
                beginAtZero: true,
                grid: { color: '#eef2f7', drawBorder: false },
                ticks: { precision: options.precision === undefined ? 0 : options.precision }
            }
        };
    }

    /* Дані графіків приходять із сервера у вигляді { labels: [], series: [{ label, data, color }] }.
   Ці функції перетворюють їх на набори даних Chart.js, щоб представлення лишалися декларативними. */
    window.AdminCharts = {
        palette: palette,

        line: function (canvasId, data, options) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || !data) { return null; }
            options = options || {};

            return new window.Chart(canvas, {
                type: 'line',
                data: {
                    labels: data.labels,
                    datasets: data.series.map(function (series, index) {
                        var color = series.color || palette[index % palette.length];
                        return {
                            label: series.label,
                            data: series.data,
                            borderColor: color,
                            backgroundColor: color + '1f',
                            borderWidth: 2,
                            pointRadius: 0,
                            pointHoverRadius: 4,
                            tension: 0.35,
                            fill: options.fill !== false
                        };
                    })
                },
                options: {
                    responsive: true,
                    interaction: { mode: 'index', intersect: false },
                    plugins: { legend: { display: data.series.length > 1, position: 'top', align: 'end' } },
                    scales: gridScales(options)
                }
            });
        },

        bar: function (canvasId, data, options) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || !data) { return null; }
            options = options || {};

            return new window.Chart(canvas, {
                type: 'bar',
                data: {
                    labels: data.labels,
                    datasets: data.series.map(function (series, index) {
                        return {
                            label: series.label,
                            data: series.data,
                            backgroundColor: options.colorPerBar
                                ? data.labels.map(function (_, i) { return palette[i % palette.length]; })
                                : (series.color || palette[index % palette.length]),
                            borderRadius: 6,
                            maxBarThickness: options.maxBarThickness || 38
                        };
                    })
                },
                options: {
                    responsive: true,
                    indexAxis: options.horizontal ? 'y' : 'x',
                    plugins: { legend: { display: data.series.length > 1, position: 'top', align: 'end' } },
                    scales: gridScales(options)
                }
            });
        },

        doughnut: function (canvasId, data) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || !data || !data.series.length) { return null; }

            return new window.Chart(canvas, {
                type: 'doughnut',
                data: {
                    labels: data.labels,
                    datasets: [{
                        data: data.series[0].data,
                        backgroundColor: data.labels.map(function (_, i) { return palette[i % palette.length]; }),
                        borderWidth: 2,
                        borderColor: '#fff'
                    }]
                },
                options: {
                    responsive: true,
                    cutout: '62%',
                    plugins: { legend: { position: 'right', align: 'center' } }
                }
            });
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        initConfirmations();
        initFilters();
        applyChartDefaults();
    });
})();

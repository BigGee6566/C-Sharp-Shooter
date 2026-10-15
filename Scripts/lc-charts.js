/*
 * Logisticore chart renderer.
 *
 * Every chart on a report or dashboard is a <canvas data-lc-chart="line|bar|hbar|donut">
 * whose data-lc-source names an inert <script type="application/json"> block written by
 * Services/ChartJson.cs. The data is read with JSON.parse - it is never evaluated - and
 * all numbers were computed on the server.
 *
 * Data shapes:
 *   line / bar / hbar : { labels: [..], series: [{ label, data: [..], tone, dashed }] }
 *   donut             : { labels: [..], values: [..], tones: [..] }
 *
 * Accessibility: series are told apart by line style or fill pattern as well as colour,
 * donut slices carry their percentage as text, and every chart's data table is rendered
 * OPEN by the server - this script collapses it only after the chart has actually drawn.
 * A chart that cannot draw (no Chart.js, bad data, unknown type) hides its empty box and
 * leaves the table showing, on screen and in print.
 */
(function () {
    'use strict';

    var TONES = {
        primary: '#002068',
        accent: '#b45309',
        success: '#15803d',
        danger: '#b91c1c',
        info: '#0369a1',
        secondary: '#b50062',
        purple: '#6d28d9',
        neutral: '#64748b'
    };
    var ORDER = ['primary', 'accent', 'success', 'danger', 'info', 'secondary', 'purple', 'neutral'];
    var DASHES = [[], [6, 4], [2, 3], [10, 4, 2, 4]];

    var reduceMotion = !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
    // minimumFractionDigits is explicit: older engines throw a RangeError for a currency
    // format whose maximumFractionDigits is below the currency's default minimum.
    var countFmt = new Intl.NumberFormat('en-ZA', { maximumFractionDigits: 0 });
    var moneyFmt = new Intl.NumberFormat('en-ZA', { style: 'currency', currency: 'ZAR', minimumFractionDigits: 0, maximumFractionDigits: 0 });
    var moneyCompact = makeCompactMoney();

    function makeCompactMoney() {
        try {
            return new Intl.NumberFormat('en-ZA', { style: 'currency', currency: 'ZAR', notation: 'compact', minimumFractionDigits: 0, maximumFractionDigits: 1 });
        } catch (e) {
            return moneyFmt;
        }
    }

    function colour(tone, i) {
        return TONES[tone] || TONES[ORDER[i % ORDER.length]];
    }

    function rgba(hex, a) {
        var n = parseInt(hex.slice(1), 16);
        return 'rgba(' + ((n >> 16) & 255) + ',' + ((n >> 8) & 255) + ',' + (n & 255) + ',' + a + ')';
    }

    /*
     * A small repeating fill so neighbouring slices/bars differ in texture, not only hue:
     * index 0 solid, then diagonal lines, dots, cross-hatch.
     */
    function patterned(hex, index) {
        var kind = index % 4;
        if (kind === 0) return hex;
        var tile = document.createElement('canvas');
        tile.width = tile.height = 8;
        var g = tile.getContext('2d');
        if (!g) return hex;
        g.fillStyle = hex;
        g.fillRect(0, 0, 8, 8);
        g.strokeStyle = 'rgba(255,255,255,0.6)';
        g.fillStyle = 'rgba(255,255,255,0.6)';
        g.lineWidth = 1.5;
        g.beginPath();
        if (kind === 1) {
            g.moveTo(0, 8); g.lineTo(8, 0);
            g.moveTo(-2, 2); g.lineTo(2, -2);
            g.moveTo(6, 10); g.lineTo(10, 6);
            g.stroke();
        } else if (kind === 2) {
            g.arc(4, 4, 1.4, 0, Math.PI * 2);
            g.fill();
        } else {
            g.moveTo(4, 0); g.lineTo(4, 8);
            g.moveTo(0, 4); g.lineTo(8, 4);
            g.stroke();
        }
        return g.createPattern(tile, 'repeat') || hex;
    }

    function fmt(v, format, compact) {
        if (v === null || v === undefined || isNaN(v)) return '-';
        if (format === 'currency') return compact ? moneyCompact.format(v) : moneyFmt.format(v);
        if (format === 'percent') return (Math.round(v * 10) / 10) + '%';
        return countFmt.format(v);
    }

    function truncate(s, max) {
        s = String(s);
        return s.length > max ? s.slice(0, max - 3) + '...' : s;
    }

    function read(id) {
        var el = document.getElementById(id);
        if (!el) return null;
        try { return JSON.parse(el.textContent); } catch (e) { return null; }
    }

    function figureOf(canvas) {
        return canvas.closest ? canvas.closest('figure') : null;
    }

    // Drew fine: the visual is there, so tuck the data table away (it stays one click away).
    function succeeded(canvas) {
        var f = figureOf(canvas);
        var d = f && f.querySelector('.lc-chart-table');
        if (d) d.open = false;
    }

    // Could not draw: hide the empty box and keep the table open.
    function failed(canvas) {
        var f = figureOf(canvas);
        if (!f) return;
        f.classList.add('lc-chart-failed');
        var d = f.querySelector('.lc-chart-table');
        if (d) d.open = true;
    }

    function fontSetup() {
        Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
        Chart.defaults.font.size = 12;
        Chart.defaults.color = '#444653';
        Chart.defaults.borderColor = '#e2e2e5';
    }

    function common(format, horizontal) {
        var valueTicks = { callback: function (v) { return fmt(v, format, true); } };
        if (format !== 'percent') valueTicks.precision = 0;

        var valueAxis = { beginAtZero: true, grid: { color: '#eeeef0' }, ticks: valueTicks };
        var categoryAxis = {
            grid: { display: false },
            ticks: {
                autoSkip: true,
                maxRotation: 0,
                // Long names (cities, clients) would eat the plot area; the full text stays in
                // the tooltip and the data table.
                callback: function (value) { return truncate(this.getLabelForValue(value), horizontal ? 18 : 14); }
            }
        };
        return {
            responsive: true,
            maintainAspectRatio: false,
            devicePixelRatio: Math.max(window.devicePixelRatio || 1, 2),
            animation: reduceMotion ? false : { duration: 350 },
            indexAxis: horizontal ? 'y' : 'x',
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 10, padding: 14 } },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            var v = horizontal ? ctx.parsed.x : ctx.parsed.y;
                            return ' ' + ctx.dataset.label + ': ' + fmt(v, format, false);
                        }
                    }
                }
            },
            scales: horizontal ? { x: valueAxis, y: categoryAxis } : { x: categoryAxis, y: valueAxis }
        };
    }

    function lineChart(canvas, d, format) {
        // Fewer than four points reads better as bars than as a line (chart guidance).
        if (!d.labels || d.labels.length < 4) return barChart(canvas, d, format, false);

        var datasets = d.series.map(function (s, i) {
            var c = colour(s.tone, i);
            return {
                label: s.label,
                data: s.data,
                borderColor: c,
                backgroundColor: rgba(c, 0.12),
                borderWidth: s.dashed ? 2 : 2.5,
                borderDash: s.dashed ? [6, 4] : DASHES[i % DASHES.length],
                pointRadius: d.labels.length > 40 ? 0 : 3,
                pointHoverRadius: 5,
                pointStyle: ['circle', 'rect', 'triangle', 'rectRot'][i % 4],
                tension: 0.25,
                fill: !s.dashed && i === 0
            };
        });
        return new Chart(canvas, { type: 'line', data: { labels: d.labels, datasets: datasets }, options: common(format, false) });
    }

    function barChart(canvas, d, format, horizontal) {
        var multi = d.series.length > 1;
        var datasets = d.series.map(function (s, i) {
            var c = colour(s.tone, i);
            return {
                label: s.label,
                data: s.data,
                // A comparison ("previous") series is a light, outlined bar; any other extra
                // series gets a fill pattern - so grouped bars never differ by hue alone.
                backgroundColor: s.dashed ? rgba(c, 0.25) : patterned(rgba(c, 0.85), i),
                borderColor: c,
                borderWidth: s.dashed || i > 0 ? 2 : 0,
                borderRadius: 4,
                maxBarThickness: 42
            };
        });
        var opts = common(format, horizontal);
        if (!multi) opts.plugins.legend.display = false;
        return new Chart(canvas, { type: 'bar', data: { labels: d.labels, datasets: datasets }, options: opts });
    }

    // Writes each visible slice's percentage onto it, so the share is readable without colour.
    var sliceLabels = {
        id: 'lcSliceLabels',
        afterDatasetsDraw: function (chart) {
            var ds = chart.data.datasets[0];
            if (!ds) return;
            var total = 0;
            ds.data.forEach(function (v, i) { if (chart.getDataVisibility(i)) total += v; });
            if (!total) return;
            var ctx = chart.ctx;
            ctx.save();
            ctx.font = '600 12px Inter, system-ui, sans-serif';
            ctx.fillStyle = '#ffffff';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            chart.getDatasetMeta(0).data.forEach(function (arc, i) {
                if (!chart.getDataVisibility(i)) return;
                var share = ds.data[i] / total;
                if (share < 0.06) return;
                var p = arc.tooltipPosition();
                ctx.fillText(Math.round(share * 100) + '%', p.x, p.y);
            });
            ctx.restore();
        }
    };

    function legendSide(width) {
        return width < 420 ? 'bottom' : 'right';
    }

    function donutChart(canvas, d, format) {
        var colours = d.labels.map(function (_, i) { return patterned(colour(d.tones && d.tones[i], i), i); });
        var total = d.values.reduce(function (a, b) { return a + b; }, 0);
        var box = canvas.parentNode;
        return new Chart(canvas, {
            type: 'doughnut',
            data: { labels: d.labels, datasets: [{ data: d.values, backgroundColor: colours, borderColor: '#ffffff', borderWidth: 2 }] },
            plugins: [sliceLabels],
            options: {
                responsive: true,
                maintainAspectRatio: false,
                devicePixelRatio: Math.max(window.devicePixelRatio || 1, 2),
                animation: reduceMotion ? false : { duration: 350 },
                cutout: '58%',
                onResize: function (chart, size) {
                    var side = legendSide(size.width);
                    if (chart.options.plugins.legend.position !== side) {
                        chart.options.plugins.legend.position = side;
                        chart.update('none');
                    }
                },
                plugins: {
                    legend: {
                        position: legendSide(box ? box.clientWidth : 600),
                        labels: {
                            usePointStyle: true, boxWidth: 10, padding: 12,
                            // Legend shows value and share as text: "Delivered - 120 (60%)".
                            generateLabels: function (chart) {
                                var base = Chart.overrides.doughnut.plugins.legend.labels.generateLabels(chart);
                                base.forEach(function (item, i) {
                                    var v = d.values[i];
                                    var pct = total ? Math.round(v / total * 100) : 0;
                                    item.text = d.labels[i] + ' - ' + fmt(v, format, false) + ' (' + pct + '%)';
                                });
                                return base;
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                var pct = total ? Math.round(ctx.parsed / total * 1000) / 10 : 0;
                                return ' ' + ctx.label + ': ' + fmt(ctx.parsed, format, false) + ' (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
    }

    var DRAW = {
        line: function (c, d, f) { return lineChart(c, d, f); },
        bar: function (c, d, f) { return barChart(c, d, f, false); },
        hbar: function (c, d, f) { return barChart(c, d, f, true); },
        donut: function (c, d, f) { return donutChart(c, d, f); }
    };

    // Fit every chart to its card's current box - used when the page switches to/from print.
    function fitAll() {
        if (typeof window.Chart === 'undefined') return;
        Object.keys(Chart.instances).forEach(function (k) {
            var chart = Chart.instances[k];
            var box = chart.canvas && chart.canvas.parentNode;
            if (box && box.clientWidth) chart.resize(box.clientWidth, box.clientHeight);
            else chart.resize();
        });
    }

    function init() {
        var canvases = document.querySelectorAll('canvas[data-lc-chart]');
        if (!canvases.length) return;

        if (typeof window.Chart === 'undefined') {
            for (var j = 0; j < canvases.length; j++) failed(canvases[j]);
            return;
        }

        fontSetup();
        for (var i = 0; i < canvases.length; i++) {
            var canvas = canvases[i];
            var draw = DRAW[canvas.getAttribute('data-lc-chart')];
            var data = read(canvas.getAttribute('data-lc-source'));
            if (!draw || !data) { failed(canvas); continue; }

            // A partial postback can re-render the same canvas; Chart.js refuses to draw
            // twice on one canvas, so release any previous instance first.
            var existing = Chart.getChart(canvas);
            if (existing) existing.destroy();

            try {
                draw(canvas, data, canvas.getAttribute('data-lc-format') || 'number');
                succeeded(canvas);
            } catch (e) {
                // One broken chart must not take the rest of the page with it.
                failed(canvas);
            }
        }
    }

    function start() {
        init();

        // Print: Chrome applies the print layout after 'beforeprint', so also listen to the
        // print media query, which changes once the print layout is in effect.
        var printQuery = window.matchMedia ? window.matchMedia('print') : null;
        if (printQuery) {
            if (printQuery.addEventListener) printQuery.addEventListener('change', fitAll);
            else if (printQuery.addListener) printQuery.addListener(fitAll);
        }
        window.addEventListener('beforeprint', fitAll);
        window.addEventListener('afterprint', fitAll);

        // Canvas text is drawn with whatever font is ready; redraw once Inter has loaded.
        if (document.fonts && document.fonts.ready && typeof window.Chart !== 'undefined') {
            document.fonts.ready.then(function () {
                Object.keys(Chart.instances).forEach(function (k) { Chart.instances[k].update('none'); });
            });
        }

        // Web Forms partial postbacks (UpdatePanel) replace markup without a page load.
        if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(init);
        }
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
})();

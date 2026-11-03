// Billing: live total on the generate-invoice form.
// total = (lines + additional charge - discount) + tax rate applied to that amount.
(function () {
    'use strict';

    function toNumber(value) {
        var parsed = parseFloat(value);
        return isNaN(parsed) ? 0 : parsed;
    }

    function round2(value) {
        return Math.round((value + Number.EPSILON) * 100) / 100;
    }

    function formatMoney(value) {
        return '\u20B9' + value.toFixed(2);
    }

    function init() {
        var totals = document.getElementById('billingTotals');
        if (!totals) {
            return;
        }

        var baseLines = toNumber(totals.getAttribute('data-lines-total'));
        var amountInput = document.querySelector('[data-billing="amount"]');
        var discountInput = document.querySelector('[data-billing="discount"]');
        var taxInput = document.querySelector('[data-billing="tax"]');

        var linesEl = document.getElementById('billingLinesTotal');
        var discountEl = document.getElementById('billingDiscount');
        var taxEl = document.getElementById('billingTax');
        var grandEl = document.getElementById('billingGrandTotal');

        function recalc() {
            var additional = Math.max(0, amountInput ? toNumber(amountInput.value) : 0);
            var linesTotal = round2(baseLines + additional);
            var discount = Math.min(linesTotal, Math.max(0, discountInput ? toNumber(discountInput.value) : 0));
            var taxRate = Math.min(100, Math.max(0, taxInput ? toNumber(taxInput.value) : 0));

            var taxable = round2(linesTotal - discount);
            var tax = round2(taxable * taxRate / 100);
            var total = round2(taxable + tax);

            if (linesEl) { linesEl.textContent = formatMoney(linesTotal); }
            if (discountEl) { discountEl.textContent = formatMoney(discount); }
            if (taxEl) { taxEl.textContent = formatMoney(tax); }
            if (grandEl) { grandEl.textContent = formatMoney(total); }

            if (discountInput) { discountInput.max = linesTotal.toFixed(2); }
        }

        [amountInput, discountInput, taxInput].forEach(function (input) {
            if (input) {
                input.addEventListener('input', recalc);
            }
        });

        recalc();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

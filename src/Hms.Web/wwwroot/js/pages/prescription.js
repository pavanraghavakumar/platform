(() => {
    "use strict";

    const lines = document.getElementById("medicine-lines");
    const template = document.getElementById("medicine-line-template");
    const addButton = document.getElementById("add-medicine-line");

    if (!lines || !template || !addButton) {
        return;
    }

    const reindex = () => {
        lines.querySelectorAll("[data-prescription-line]").forEach((line, index) => {
            line.querySelectorAll("[data-field]").forEach((field) => {
                const fieldName = field.dataset.field;
                field.name = `Items[${index}].${fieldName}`;
                field.id = `Items_${index}__${fieldName}`;
                const label = field.closest(".hms-field")?.querySelector("label");
                if (label) {
                    label.htmlFor = field.id;
                }
            });
        });
    };

    const addLine = () => {
        lines.appendChild(template.content.cloneNode(true));
        reindex();
        lines.lastElementChild?.querySelector("select")?.focus();
    };

    addButton.addEventListener("click", addLine);
    lines.addEventListener("click", (event) => {
        const removeButton = event.target.closest("[data-remove-line]");
        if (!removeButton) {
            return;
        }

        removeButton.closest("[data-prescription-line]")?.remove();
        reindex();
    });

    reindex();
})();

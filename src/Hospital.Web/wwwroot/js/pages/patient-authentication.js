(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        const input = document.getElementById(button.dataset.passwordToggle);
        if (!input) return;

        button.addEventListener("click", () => {
            const reveal = input.type === "password";
            input.type = reveal ? "text" : "password";
            button.textContent = reveal ? "Hide" : "Show";
            button.setAttribute("aria-label", reveal
                ? `Hide ${input.labels?.[0]?.textContent?.toLowerCase() || "password"}`
                : `Show ${input.labels?.[0]?.textContent?.toLowerCase() || "password"}`);
        });
    });

    const registrationForm = document.querySelector("form[action='/patient/register']");
    const password = registrationForm?.querySelector("#Password");
    const confirmation = registrationForm?.querySelector("#ConfirmPassword");
    if (password && confirmation) {
        const validateConfirmation = () => {
            confirmation.setCustomValidity(
                confirmation.value && confirmation.value !== password.value
                    ? "Passwords do not match."
                    : "");
        };

        password.addEventListener("input", validateConfirmation);
        confirmation.addEventListener("input", validateConfirmation);
        registrationForm.addEventListener("submit", (event) => {
            validateConfirmation();

            const invalidFields = Array.from(registrationForm.elements)
                .filter((field) => field instanceof HTMLInputElement && !field.validity.valid);
            if (invalidFields.length === 0) return;

            event.preventDefault();
            const summary = document.querySelector("[data-validation-summary='true']");
            const summaryList = summary?.querySelector("ul");
            if (summaryList) summaryList.replaceChildren();

            registrationForm.querySelectorAll("[aria-invalid='true']").forEach((field) => {
                field.removeAttribute("aria-invalid");
            });
            registrationForm.querySelectorAll("[id$='-error']").forEach((fieldError) => {
                fieldError.textContent = "";
            });

            invalidFields.forEach((field) => {
                field.setAttribute("aria-invalid", "true");
                const message = field.validationMessage;
                const fieldError = document.getElementById(`${field.id}-error`);
                if (fieldError) fieldError.textContent = message;

                if (summaryList) {
                    const item = document.createElement("li");
                    const link = document.createElement("a");
                    link.href = `#${field.id}`;
                    link.textContent = message;
                    item.append(link);
                    summaryList.append(item);
                }
            });

            if (summary) summary.hidden = false;
            invalidFields[0].focus();
        });
    }

    const summary = document.querySelector("[data-validation-summary='true']");
    if (summary && !summary.hidden) summary.focus();
})();

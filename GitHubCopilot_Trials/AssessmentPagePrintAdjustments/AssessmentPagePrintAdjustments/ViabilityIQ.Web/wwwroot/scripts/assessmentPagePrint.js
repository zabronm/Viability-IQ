(function () {
    "use strict";

    function printPage(targetSelector) {
        const target = document.querySelector(targetSelector);
        if (!target) {
            throw new Error(`Print target '${targetSelector}' was not found.`);
        }

        const collapses = Array.from(target.querySelectorAll(".accordion-collapse"));
        const buttons = Array.from(target.querySelectorAll(".accordion-button"));
        const collapseStates = collapses.map(element => ({
            element,
            wasShown: element.classList.contains("show")
        }));
        const buttonStates = buttons.map(element => ({
            element,
            wasCollapsed: element.classList.contains("collapsed"),
            ariaExpanded: element.getAttribute("aria-expanded")
        }));

        collapses.forEach(element => element.classList.add("show"));
        buttons.forEach(element => {
            element.classList.remove("collapsed");
            element.setAttribute("aria-expanded", "true");
        });

        const pageStyle = document.createElement("style");
        pageStyle.textContent = "@page { size: landscape; margin: 8mm; }";
        document.head.appendChild(pageStyle);
        document.body.classList.add("assessment-page-printing");

        return new Promise(resolve => {
            let completed = false;

            const cleanup = () => {
                if (completed) return;
                completed = true;

                collapseStates.forEach(state => {
                    state.element.classList.toggle("show", state.wasShown);
                });
                buttonStates.forEach(state => {
                    state.element.classList.toggle("collapsed", state.wasCollapsed);
                    if (state.ariaExpanded === null) {
                        state.element.removeAttribute("aria-expanded");
                    } else {
                        state.element.setAttribute("aria-expanded", state.ariaExpanded);
                    }
                });

                document.body.classList.remove("assessment-page-printing");
                pageStyle.remove();
                resolve();
            };

            window.addEventListener("afterprint", cleanup, { once: true });
            window.setTimeout(cleanup, 60000);
            window.requestAnimationFrame(() => {
                window.requestAnimationFrame(() => window.print());
            });
        });
    }

    window.viqAssessmentPagePrint = {
        print: printPage
    };
})();

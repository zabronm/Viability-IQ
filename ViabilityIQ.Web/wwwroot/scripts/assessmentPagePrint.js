(function () {
    "use strict";

    function waitForPrintResources(printDocument) {
        const stylesheets = Array.from(
            printDocument.querySelectorAll('link[rel="stylesheet"]'));
        const stylesReady = Promise.all(stylesheets.map(stylesheet =>
            new Promise(resolve => {
                if (stylesheet.sheet) {
                    resolve();
                    return;
                }

                stylesheet.addEventListener("load", resolve, { once: true });
                stylesheet.addEventListener("error", resolve, { once: true });
                window.setTimeout(resolve, 3000);
            })));
        const fontsReady = printDocument.fonts?.ready ?? Promise.resolve();

        return Promise.all([stylesReady, fontsReady]);
    }

    function printPage(targetSelector) {
        const target = document.querySelector(targetSelector);
        if (!target) {
            throw new Error(`Print target '${targetSelector}' was not found.`);
        }

        const printFrame = document.createElement("iframe");
        printFrame.setAttribute("aria-hidden", "true");
        printFrame.style.position = "fixed";
        printFrame.style.right = "0";
        printFrame.style.bottom = "0";
        printFrame.style.width = "1px";
        printFrame.style.height = "1px";
        printFrame.style.border = "0";
        printFrame.style.opacity = "0";
        document.body.appendChild(printFrame);

        const printDocument = printFrame.contentDocument;
        if (!printDocument || !printFrame.contentWindow) {
            printFrame.remove();
            throw new Error("The print document could not be created.");
        }

        const styles = Array.from(
            document.querySelectorAll('link[rel="stylesheet"], style'))
            .map(element => element.outerHTML)
            .join("");
        const title = document.title || "ViabilityIQ Sensitivity Analysis";

        printDocument.open();
        printDocument.write(`<!doctype html>
<html>
<head>
    <base href="${document.baseURI}">
    <title></title>
    ${styles}
    <style>
        @page { size: landscape; margin: 8mm; }
        html, body {
            width: 100% !important;
            height: auto !important;
            min-height: 0 !important;
            margin: 0 !important;
            overflow: visible !important;
            background: #fff !important;
        }
    </style>
</head>
<body class="assessment-page-printing">${target.outerHTML}</body>
</html>`);
        printDocument.close();
        printDocument.title = title;

        return waitForPrintResources(printDocument).then(() =>
            new Promise(resolve => {
                let completed = false;
                const cleanup = () => {
                    if (completed) return;
                    completed = true;
                    printFrame.remove();
                    resolve();
                };

                printFrame.contentWindow.addEventListener(
                    "afterprint",
                    cleanup,
                    { once: true });
                window.setTimeout(cleanup, 60000);
                window.requestAnimationFrame(() => {
                    window.requestAnimationFrame(() => {
                        printFrame.contentWindow.focus();
                        printFrame.contentWindow.print();
                    });
                });
            }));
    }

    window.viqAssessmentPagePrint = {
        print: printPage
    };
})();

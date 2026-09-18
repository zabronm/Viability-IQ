(function () {
    "use strict";

    function downloadBase64(fileName, contentType, base64) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function printElement(selector, title, landscape) {
        const source = document.querySelector(selector);
        if (!source) throw new Error(`Report element '${selector}' was not found.`);
        const frame = document.createElement("iframe");
        frame.setAttribute("aria-hidden", "true");
        frame.style.position = "fixed";
        frame.style.right = "0";
        frame.style.bottom = "0";
        frame.style.width = "0";
        frame.style.height = "0";
        frame.style.border = "0";
        document.body.appendChild(frame);
        const doc = frame.contentDocument;
        const styles = Array.from(document.querySelectorAll('link[rel="stylesheet"],style'))
            .map(node => node.outerHTML).join("");
        doc.open();
        doc.write(`<!doctype html><html><head><base href="${escapeHtml(document.baseURI)}"><title>${escapeHtml(title)}</title>${styles}` +
            `<style>@page{size:${landscape ? "A4 landscape" : "A4 portrait"};margin:9mm}` +
            `html,body{margin:0;background:#fff}.report-screen-only{display:none!important}</style>` +
            `</head><body>${source.outerHTML}</body></html>`);
        doc.close();
        return waitForImages(doc).then(() => new Promise((resolve, reject) => {
            window.setTimeout(() => {
                try {
                    frame.contentWindow.focus();
                    frame.contentWindow.print();
                    window.setTimeout(() => { frame.remove(); resolve(); }, 500);
                } catch (error) {
                    frame.remove();
                    reject(error);
                }
            }, 100);
        }));
    }

    function waitForImages(doc) {
        const images = Array.from(doc.images);
        return Promise.all(images.map(image => {
            if (image.complete) return Promise.resolve();
            return new Promise(resolve => {
                image.addEventListener("load", resolve, { once: true });
                image.addEventListener("error", resolve, { once: true });
            });
        }));
    }

    function escapeHtml(value) {
        const div = document.createElement("div");
        div.textContent = value || "Report";
        return div.innerHTML;
    }

    window.viqReportOutput = { downloadBase64, printElement };
})();

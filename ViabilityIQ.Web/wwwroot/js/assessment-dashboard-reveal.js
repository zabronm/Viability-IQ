(() => {
    const selector = ".dashboard-reveal";
    const canObserve = "IntersectionObserver" in window;
    const observer = canObserve
        ? new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (!entry.isIntersecting && entry.intersectionRatio <= 0) {
                    continue;
                }

                entry.target.classList.add("reveal-visible");
                observer.unobserve(entry.target);
            }
        }, { threshold: 0.08, rootMargin: "0px 0px -6% 0px" })
        : null;

    const watch = element => {
        if (element.classList.contains("reveal-observed")) {
            return;
        }

        element.classList.add("reveal-observed");
        if (observer) {
            observer.observe(element);
        } else {
            element.classList.add("reveal-visible");
        }
    };

    const scan = node => {
        if (!(node instanceof Element) && !(node instanceof DocumentFragment)) {
            return;
        }

        if (node instanceof Element && node.matches(selector)) {
            watch(node);
        }

        node.querySelectorAll(selector).forEach(watch);
    };

    const unwatch = node => {
        if (!(node instanceof Element) && !(node instanceof DocumentFragment)) {
            return;
        }

        if (node instanceof Element && node.matches(selector)) {
            observer?.unobserve(node);
        }

        node.querySelectorAll(selector).forEach(element => observer?.unobserve(element));
    };

    scan(document);

    if ("MutationObserver" in window && document.documentElement) {
        const mutationObserver = new MutationObserver(records => {
            for (const record of records) {
                record.addedNodes.forEach(scan);
                record.removedNodes.forEach(unwatch);
            }
        });

        mutationObserver.observe(document.documentElement, { childList: true, subtree: true });
    }
})();

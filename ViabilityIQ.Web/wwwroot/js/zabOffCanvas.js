
window.zabOffCanvas = {

    show: function (id) {
        
        const element = document.getElementById(id);

        if (!element)
            return;

        let instance = bootstrap.Offcanvas.getInstance(element);

        if (!instance) {

            instance = new bootstrap.Offcanvas(element, {

                backdrop: 'static',

                keyboard: false,

                scroll: false

            });
        }

        instance.show();

        window.setTimeout(() => {

            this.focusFirstInput(id);

        }, 250);
    },

    hide: function (id) {

        const element = document.getElementById(id);

        if (!element)
            return;

        const instance = bootstrap.Offcanvas.getInstance(element);

        if (!instance)
            return;

        element.addEventListener('hidden.bs.offcanvas', function handler() {

            instance.dispose();

            element.removeEventListener('hidden.bs.offcanvas', handler);

        }, { once: true });

        instance.hide();
    },

    focusFirstInput: function (id) {

    const element = document.getElementById(id);

    if (!element)
        return;

    setTimeout(() => {

        const control = element.querySelector(

            "input:not([type='hidden'])," +

            "select," +

            "textarea"

        );

        if (control)
            control.focus();

    }, 200);

}
};
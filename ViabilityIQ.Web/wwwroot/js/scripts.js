/*!
    * Start Bootstrap - SB Admin v7.0.7 (https://startbootstrap.com/template/sb-admin)
    * Copyright 2013-2023 Start Bootstrap
    * Licensed under MIT (https://github.com/StartBootstrap/startbootstrap-sb-admin/blob/master/LICENSE)
    */
//
// Scripts
// 
console.log("scripts.js loaded");

window.addEventListener('DOMContentLoaded', event => {

    // Toggle the side navigation
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        // Uncomment Below to persist sidebar toggle between refreshes
        if (localStorage.getItem('sb|sidebar-toggle') === 'true') {
            document.body.classList.toggle('sb-sidenav-toggled');
        }
        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');
            localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
        });
    }

});

//This is for the side-bar 
window.toggleSidebar = () => {
    document.body.classList.toggle('sb-sidenav-toggled');
};

//This is for the top bar date/time display
window.browserTime = {
    getCurrentLocalTime: function () {
        const d = new Date();
        const day = String(d.getDate()).padStart(2, '0');
        const month = d.toLocaleString('en-US', { month: 'short' }).toUpperCase();
        const year = d.getFullYear();
        const hour = String(d.getHours()).padStart(2, '0');
        const minute = String(d.getMinutes()).padStart(2, '0');

        const fullTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        const shortTimeZone = fullTimeZone.split('/').pop().replaceAll('_', ' ');

        return `${day} ${month} ${year} | ${hour}:${minute} (${shortTimeZone})  `;
    }
};

//This is for the OffCanvas component

window.ui = {
    showOffcanvas: function (id) {

        const el = document.getElementById(id);

        if (!el) {
            console.error("Offcanvas not found:", id);
            return;
        }

        let offcanvas = bootstrap.Offcanvas.getInstance(el);

        if (!offcanvas) {
            offcanvas = new bootstrap.Offcanvas(el,
                {
                    backdrop: 'static',
                    keyboard: false
                });
        }
        offcanvas.show();
    },


    hideOffcanvas: function (id) {

        const el =
            document.getElementById(id);

        if (!el) return;

        const offcanvas =
            bootstrap.Offcanvas.getInstance(el);

        if (offcanvas) {
            offcanvas.hide();
        }
    },


    showToast: function (id) {
        const el = document.getElementById(id);
        if (!el) {
            console.error("Toast not found:", id);
            return;
        }

        const toast = new bootstrap.Toast(el);
        toast.show();
    },

    //This handles the universal Bootstrap tooltip 
    initTooltip: function (element) {
        if (element) {
            new bootstrap.Tooltip(element, {
                // This allows the 'data-bs-custom-class' attribute to work
                customClass: element.getAttribute('data-bs-custom-class')
            });
        }
    }
};


window.zabDropdown = {

    registerClickOutside: function (element, dotnetRef) {
        document.addEventListener('click', function (e) {
            if (!element.contains(e.target)) {
                dotnetRef.invokeMethodAsync('CloseDropdown');
            }
        });
    }
};


window.toastInterop = {
    show: function (id) {
        console.log("Toast called:", id);

        const el = document.getElementById(id);
        if (!el) {
            console.error("Toast element not found");
            return;
        }

        const toast = bootstrap.Toast.getOrCreateInstance(el);
        toast.show();
    }
};



// ============================================
// OPEN PDF IN NEW TAB (PRINT PREVIEW)
// ============================================

window.openPdfInNewTab = (base64Data) => {

    // CREATE NEW TAB
    const pdfWindow = window.open("");

    // SAFETY CHECK
    if (!pdfWindow) {

        alert("Popup blocked. Please allow popups.");
        return;
    }

    // WRITE HTML INTO NEW TAB
    pdfWindow.document.write(`
        <html>

            <head>
                <title>Report Preview</title>
                <style>
                    html, body {
                        margin: 0;
                        padding: 0;
                        width: 100%;
                        height: 100%;
                        overflow: hidden;
                    }
                    iframe {
                        border: none;
                        width: 100%;
                        height: 100%;
                    }
                </style>
            </head>
            <body>
                <iframe
                    src="data:application/pdf;base64,${base64Data}">
                </iframe>
            </body>
        </html>
    `);

    pdfWindow.document.close();
};




// ============================================
// DOWNLOAD FILE
// ============================================

window.downloadFile = (fileName, base64Data, contentType) => {
    const link = document.createElement('a');                        // CREATE LINK
    link.download = fileName;                                       // SET DOWNLOAD NAME    
    link.href = `data:${contentType};base64,${base64Data}`;         // SET FILE CONTENT   
    document.body.appendChild(link);                                // ADD TO PAGE   
    link.click();                                                  // TRIGGER DOWNLOAD   
    document.body.removeChild(link);                               // CLEANUP
};


window.ZabFileSaver = {
    DownloadBinaryStream: function (fileName, base64ContentString) {
        var link = document.createElement('a');
        link.download = fileName;
        link.href = "data:application/octet-stream;base64," + base64ContentString;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};





//FILE DRAG  DROP - Germini
window.blazorDropZone = {
    initFileTransfer: function (dropZoneId, inputId) {
        const dropZone = document.getElementById(dropZoneId);
        const input = document.getElementById(inputId);

        if (!dropZone || !input) return;

        const handler = (e) => {
            // Assign the dropped file array metadata index token straight into the input file collection
            input.files = e.dataTransfer.files;

            // Dispatch a change event so Blazor's native OnChange="LoadFiles" event triggers naturally!
            const event = new Event('change', { bubbles: true });
            input.dispatchEvent(event);
        };

        // Listen once to catch the current dropping action stream context securely
        dropZone.addEventListener('drop', handler, { once: true });
    }
};
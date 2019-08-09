window.readFileAsBase64 = (fileInput) => {
    const readAsDataURL = (fileInput) => {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onerror = () => {
                reader.abort();
                reject(new Error("Error reading file."));
            };
            reader.addEventListener("load", () => {
                resolve(reader.result);
            }, false);
            reader.readAsDataURL(fileInput.files[0]);
        });
    };

    return readAsDataURL(fileInput);
};

window.getPopupWidthAndTop = (popup, sibling) => {
    var el = sibling ? popup.previousElementSibling : popup.parentNode;
    var elHeight = sibling ? popup.getBoundingClientRect().height : 0;
    var rect = el.getBoundingClientRect();
    var display = popup.style.display == 'none' ? 'display:none;' : '';
    return `width:calc(${rect.width + 'px'} - 1.425rem); top:${rect.top + rect.height + elHeight - 3 + 'px'}; ${display}`;
};

window.getInputValue = (arg) => {
    var input = (arg instanceof Element || arg instanceof HTMLDocument) ? arg : document.getElementById(arg);
    return input ? input.value : '';
};

window.closePopup = (e, el) => {
    if (!el.contains(e.relatedTarget) && el.children[1]) {
        el.children[1].style.display = 'none';
    }
};

window.openPopup = (e, el) => {
    const handler = () => {
        popup.style.display = 'none';
        window.removeEventListener('wheel', handler);
    };
    window.addEventListener('wheel', handler);

    var popup = el.parentNode.parentNode.children[1];
    popup.style.cssText = popup.style.display == 'none' ?
        `margin-left: 0px; z-index: 1002; transform: translateY(0px); opacity: 1; position: fixed; ${getPopupWidthAndTop(popup).replace('display:none;', '')};width:320px;`
        : 'display:none;';
};
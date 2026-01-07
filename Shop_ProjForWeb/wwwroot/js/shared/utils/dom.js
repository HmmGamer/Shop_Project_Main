/**
 * DOM helper utilities
 */

export const $ = (selector, context = document) => {
    return context.querySelector(selector);
};

export const $$ = (selector, context = document) => {
    return Array.from(context.querySelectorAll(selector));
};

export const createElement = (tag, attributes = {}, children = []) => {
    const element = document.createElement(tag);

    Object.entries(attributes).forEach(([key, value]) => {
        if (key === 'className') {
            element.className = value;
        } else if (key === 'dataset') {
            Object.entries(value).forEach(([dataKey, dataValue]) => {
                element.dataset[dataKey] = dataValue;
            });
        } else if (key.startsWith('on') && typeof value === 'function') {
            element.addEventListener(key.slice(2).toLowerCase(), value);
        } else {
            element.setAttribute(key, value);
        }
    });

    children.forEach(child => {
        if (typeof child === 'string') {
            element.appendChild(document.createTextNode(child));
        } else if (child instanceof Node) {
            element.appendChild(child);
        }
    });

    return element;
};

export const delegate = (container, selector, event, handler) => {
    container.addEventListener(event, (e) => {
        const target = e.target.closest(selector);
        if (target && container.contains(target)) {
            handler.call(target, e, target);
        }
    });
};

export const show = (element) => {
    if (element) element.style.display = '';
};

export const hide = (element) => {
    if (element) element.style.display = 'none';
};

export const toggle = (element, condition) => {
    if (element) {
        element.style.display = condition ? '' : 'none';
    }
};

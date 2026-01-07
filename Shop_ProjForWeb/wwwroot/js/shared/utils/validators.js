/**
 * Validation utility functions
 */

export const validateEmail = (email) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
};

export const validatePhoneNumber = (phone) => {
    const phoneRegex = /^\+?[\d\s\-\(\)]{10,}$/;
    return phoneRegex.test(phone);
};

export const validateProductData = (data) => {
    const errors = {};

    if (!data.name || data.name.trim().length < 2) {
        errors.name = 'Product name must be at least 2 characters';
    }

    if (!data.basePrice || data.basePrice <= 0) {
        errors.basePrice = 'Base price must be greater than 0';
    }

    if (data.discountPercent < 0 || data.discountPercent > 100) {
        errors.discountPercent = 'Discount percent must be between 0 and 100';
    }

    return {
        isValid: Object.keys(errors).length === 0,
        errors
    };
};

export const validateUserData = (data) => {
    const errors = {};

    if (!data.fullName || data.fullName.trim().length < 2) {
        errors.fullName = 'Full name must be at least 2 characters';
    }

    return {
        isValid: Object.keys(errors).length === 0,
        errors
    };
};

export const validateForm = (form) => {
    const errors = {};
    const formData = new FormData(form);

    // Required fields
    const requiredFields = form.querySelectorAll('[required]');
    requiredFields.forEach(field => {
        const value = formData.get(field.name);
        if (!value || value.trim() === '') {
            errors[field.name] = 'This field is required';
        }
    });

    // Email validation
    const emailFields = form.querySelectorAll('input[type="email"]');
    emailFields.forEach(field => {
        const value = formData.get(field.name);
        if (value && !validateEmail(value)) {
            errors[field.name] = 'Please enter a valid email address';
        }
    });

    // Number validation
    const numberFields = form.querySelectorAll('input[type="number"]');
    numberFields.forEach(field => {
        const value = formData.get(field.name);
        if (value && isNaN(value)) {
            errors[field.name] = 'Please enter a valid number';
        }
        if (field.min && value < field.min) {
            errors[field.name] = `Value must be at least ${field.min}`;
        }
        if (field.max && value > field.max) {
            errors[field.name] = `Value must be at most ${field.max}`;
        }
    });

    return {
        isValid: Object.keys(errors).length === 0,
        errors
    };
};

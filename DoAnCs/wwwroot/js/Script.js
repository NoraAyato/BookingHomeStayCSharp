// Initialize AOS animation
AOS.init({
    duration: 800,
    easing: 'ease-in-out',
    once: true,
    offset: 100
});

// Initialize Splide carousels
document.addEventListener('DOMContentLoaded', function () {
    // Gallery Carousel
    new Splide('#gallery .splide', {
        type: 'loop',
        perPage: 3,
        perMove: 1,
        gap: '1.5rem',
        pagination: false,
        breakpoints: {
            1024: {
                perPage: 2,
            },
            640: {
                perPage: 1,
            }
        }
    }).mount();

    // Testimonials Carousel
    new Splide('#testimonials .splide', {
        type: 'loop',
        perPage: 1,
        perMove: 1,
        gap: '1.5rem',
        pagination: false,
        arrows: false,
        autoplay: true,
        interval: 5000,
        pauseOnHover: true
    }).mount();
});

// Back to top button
const backToTopButton = document.getElementById('backToTop');

window.addEventListener('scroll', () => {
    if (window.pageYOffset > 300) {
        backToTopButton.classList.remove('opacity-0', 'invisible');
        backToTopButton.classList.add('opacity-100', 'visible');
    } else {
        backToTopButton.classList.remove('opacity-100', 'visible');
        backToTopButton.classList.add('opacity-0', 'invisible');
    }
});

backToTopButton.addEventListener('click', () => {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
});

// Mobile menu toggle
const mobileMenuButton = document.querySelector('.mobile-menu-button');
const mobileMenu = document.querySelector('.mobile-menu');

mobileMenuButton.addEventListener('click', () => {
    mobileMenu.classList.toggle('hidden');
});

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();

        document.querySelector(this.getAttribute('href')).scrollIntoView({
            behavior: 'smooth'
        });
    });
});

// Particle animation
const particles = document.querySelectorAll('.particles li');
particles.forEach(particle => {
    const randomX = Math.random() * 100;
    const randomY = Math.random() * 1000 + 1000;
    const randomSize = Math.random() * 20 + 10;
    const randomDuration = Math.random() * 20 + 10;

    particle.style.setProperty('--random-x', `${randomX}%`);
    particle.style.setProperty('--random-y', `-${randomY}px`);
    particle.style.width = `${randomSize}px`;
    particle.style.height = `${randomSize}px`;
    particle.style.animationDuration = `${randomDuration}s`;
});



// Counter animation
document.addEventListener('DOMContentLoaded', function () {
    const counter = document.querySelector('.animate-count');
    const target = parseInt(counter.getAttribute('data-count'));
    const duration = 2000; // 2 seconds
    const step = target / (duration / 16); // 60fps

    let current = 0;

    const updateCounter = () => {
        current += step;
        if (current < target) {
            counter.textContent = '+' + Math.floor(current);
            requestAnimationFrame(updateCounter);
        } else {
            counter.textContent = '+' + target;
        }
    };

    // Start counter when element is in viewport
    const observer = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting) {
            updateCounter();
            observer.unobserve(counter);
        }
    });

    observer.observe(counter);
});
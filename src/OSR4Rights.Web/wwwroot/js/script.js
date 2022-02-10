const menuButton = document.getElementById('menu-btn');
const mobileMenu = document.getElementById('mobile-menu');

function navToggle() {
    // adds / removes open class from menu-btn element
    menuButton.classList.toggle('open');
    mobileMenu.classList.toggle('hidden');
    document.body.classList.toggle('no-scroll');
}

menuButton.addEventListener('click', navToggle)

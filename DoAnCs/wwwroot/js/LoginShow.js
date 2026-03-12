//                        |
//                        |
//Slideshow image script \ /
//                        .
//let slideIndex = 0;
//showSlides();

//function showSlides() {
//    let i;
//    let slides = document.getElementsByClassName("slideImages");
//    for (i = 0; i < slides.length; i++) {
//        slides[i].classList.remove("show");
//    }
//    slideIndex++;
//    if (slideIndex > slides.length) { slideIndex = 1 }
//    let currentSlide = slides[slideIndex - 1];
//    let nextSlide = slides[(slideIndex) % slides.length];

//    currentSlide.classList.add("show");
//    nextSlide.style.left = "100%"; // Starting position for next slide
//    nextSlide.classList.add("show");
//    nextSlide.style.transition = "left 1.5s ease-in-out";
//    currentSlide.style.transition = "left 1.5s ease-in-out";

//    setTimeout(() => {
//        currentSlide.style.left = "-100%"; // Move current slide out
//        nextSlide.style.left = "0"; // Move next slide in
//    }, 50);

//    setTimeout(showSlides, 6000); // Change image every 4 seconds
//}

//Scrolling
//document.getElementById("scrollButtonHome").addEventListener("click", function () {
//    document.getElementById("targetSectionHome").scrollIntoView({ behavior: "smooth" });
//});

//document.getElementById("scrollButtonList").addEventListener("click", function () {
//    document.getElementById("targetSectionList").scrollIntoView({ behavior: "smooth" });
//});

//document.getElementById("scrollButtonServices").addEventListener("click", function () {
//    document.getElementById("targetSectionServices").scrollIntoView({ behavior: "smooth" });
//});

//document.getElementById("scrollButtonBlog").addEventListener("click", function () {
//    document.getElementById("targetSectionBlog").scrollIntoView({ behavior: "smooth" });
//});


var modal = document.getElementById("loginModal");
var btn = document.getElementById("openModalBtn");
var span = document.getElementsByClassName("close")[0];

btn.onclick = function () {
    modal.style.display = "flex";
}

span.onclick = function () {
    modal.style.display = "none";
}

window.onclick = function (event) {
    if (event.target == modal) {
        modal.style.display = "none";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    var avatar = document.querySelector("img[alt='Avatar']");
    var dropdown = avatar.nextElementSibling; // Lấy danh sách ul ngay sau avatar

    avatar.addEventListener("mouseenter", function () {
        dropdown.style.display = "block";
    });

    dropdown.addEventListener("mouseleave", function () {
        dropdown.style.display = "none";
    });
});

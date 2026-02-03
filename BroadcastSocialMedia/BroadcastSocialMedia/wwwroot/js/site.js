// Animation för like-knappar
document.addEventListener('DOMContentLoaded', function () {
    // Like-knappar
    const likeButtons = document.querySelectorAll('.like-btn');
    likeButtons.forEach(button => {
        button.addEventListener('click', function () {
            this.classList.toggle('liked');

            // Animera hjärtat
            const heart = this.querySelector('.heart');
            if (heart) {
                heart.style.transform = 'scale(1.5)';
                setTimeout(() => {
                    heart.style.transform = 'scale(1)';
                }, 300);
            }
        });
    });

    // Ladda upp bild förhandsvisning
    const imageUpload = document.getElementById('imageUpload');
    if (imageUpload) {
        imageUpload.addEventListener('change', function (e) {
            const file = e.target.files[0];
            const preview = document.getElementById('imagePreview');

            if (file && preview) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    preview.src = e.target.result;
                    preview.style.display = 'block';
                }
                reader.readAsDataURL(file);
            }
        });
    }
});
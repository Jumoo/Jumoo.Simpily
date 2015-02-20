//
// Jumoo.Simpily.Forums - Javascript helpers and editor setup
//
$(document).ready(function () {
    tinymce.init({
        selector: 'textarea',
        plugins: [
            ["link lists anchor"]
        ],
        theme: "modern",
        skin_url: "/scripts/JumooSimpily/editorSkin",
        schema: "html5",
        toolbar: "bold italic | bullist numlist | indent outdent | link",
        statusbar: false,
        menubar: false
    });


    $(".post-delete").click(function (e) {
        e.stopPropagation();
        e.preventDefault();
        var postId = $(this).data("postid");
        deletePost(postId)
    });
});

function deletePost(postId) {
    if (window.confirm("Are you sure you want to delete this post?")) {
        $.ajax({
            url: '/Umbraco/Api/SimpilyForumsApi/DeletePost/' + postId,
            type: 'DELETE',
            success: function (data) {
                $('#post_' + postId).fadeOut();
            },
            error: function (data) {
                alert("sorry, there was an error deleting this post")
            }
        });
    }
}
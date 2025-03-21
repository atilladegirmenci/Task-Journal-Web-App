function deleteTodo(i)
{
    $.ajax({
        url: 'Home/Delete',
        type: 'POST',
        data: {
            id: i
        },
        success: function () {
            window.location.reload();
        },

    });
}

function populateForm(i) {
    $.ajax({
        url: '/Home/PopulateForm',
        type: 'GET',
        data: { id: i },
        dataType: 'json',
        success: function (response) {
            console.log("✅ PopulateForm Response:", response);  // Debugging

            $("#Todo_Name").val(response.name);
            $("#Todo_Id").val(response.id);
            $("#todo-form-button").text("Update Todo");
            $("#todoForm").attr("action", "/Home/UpdateToDo");
        },
        error: function (xhr, status, error) {
            console.log("❌ Error fetching todo:", error);
        }
    });

}
function deleteNote(i) {
    $.ajax({
        url: '/Home/DeleteNote',
        type: 'POST',
        data: { id: i },
        success: function () {
            window.location.reload();
        },
        error: function (xhr, status, error) {
            console.log("❌ Error deleting note:", error);
        }
    });
}

function editNote(i) {
    $.ajax({
        url: '/Home/GetNoteById',  // Notun verisini almak için GET isteği
        type: 'GET',
        data: { id: i },
        dataType: 'json',
        success: function (response) {
            console.log("✅ Note data fetched:", response);

            $("#Note_Title").val(response.title);
            $("#Note_Content").val(response.content);
            $("#Note_Id").val(response.id);
            $("#note-form-button").text("Update Note");
            $("#noteForm").attr("action", "/Home/UpdateNote");
        },
        error: function (xhr, status, error) {
            console.log("❌ Error fetching note:", error);
        }
    });
}
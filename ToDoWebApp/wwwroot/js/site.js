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

            // Populate the main task's name
            $("#Todo_Name").val(response.name);
            $("#Todo_Id").val(response.id);
            $("#todo-form-button").text("Update Todo"); // Change the button text to 'Update Todo'
            $("#todoForm").attr("action", "/Home/UpdateToDo"); // Change the form action to update

            // Clear existing subtasks container to repopulate
            $("#subtasks-container").empty();

            const subtaskContainer = document.getElementById("subtasks-container");

            // Populate subtasks if any exist
            let subtaskCount = 0;  // Start counting from 0 for indexing
            response.subTasks.forEach(subtask => {
                console.log("Subtask Content:", subtask.content);

                const newSubtaskDiv = document.createElement("div");
                newSubtaskDiv.classList.add("mb-3", "subtask");

                const newSubtaskLabel = document.createElement("label");
                newSubtaskLabel.classList.add("form-label");
                newSubtaskLabel.textContent = "Subtask";

                const newSubtaskInput = document.createElement("input");
                newSubtaskInput.type = "text";
                newSubtaskInput.classList.add("form-control");
                newSubtaskInput.name = `ToDoItem.SubTasks[${subtaskCount}].Content`; // Dynamically set the name attribute
                newSubtaskInput.value = subtask.content; // Populate the input with the existing subtask content
                newSubtaskInput.placeholder = "Enter subtask";
                
                const removeBtn = document.createElement("button");
                removeBtn.type = "button";
                removeBtn.classList.add("btn", "btn-danger", "mt-2");
                removeBtn.textContent = "-";
                removeBtn.addEventListener("click", function () {
                    subtaskContainer.removeChild(newSubtaskDiv);
                });

                newSubtaskDiv.appendChild(newSubtaskLabel);  // Add label
                newSubtaskDiv.appendChild(newSubtaskInput);  // Add input field
                newSubtaskDiv.appendChild(removeBtn);       // Add remove button
                subtaskContainer.appendChild(newSubtaskDiv); // Append the new subtask div to the container

                subtaskCount++; // Increment subtask count for next input
            });

            // Show the "Add Subtask" button again in case it's hidden when editing
            $("#add-subtask").show();
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

function updateSubtaskIndexes() {
    const subtaskInputs = document.querySelectorAll("#subtasks-container .subtask input");
    subtaskInputs.forEach((input, index) => {
        input.name = `ToDoItem.SubTasks[${index}].Content`;
    });
}
document.addEventListener("DOMContentLoaded", function () {
    console.log("🚀 Script Loaded!");

    let subtaskCount = 1;

    const addSubtaskBtn = document.getElementById("add-subtask");
    const subtaskContainer = document.getElementById("subtasks-container");

    if (!addSubtaskBtn || !subtaskContainer) {
        console.error("❌ Button or subtask container not found!");
        return;
    }

    addSubtaskBtn.addEventListener("click", function () {
        console.log("✅ Add Subtask button clicked");

        const newSubtaskDiv = document.createElement("div");
        newSubtaskDiv.classList.add("mb-3", "subtask");

        const newSubtaskLabel = document.createElement("label");
        newSubtaskLabel.classList.add("form-label");
        newSubtaskLabel.textContent = "Subtask";

        const newSubtaskInput = document.createElement("input");
        newSubtaskInput.type = "text";
        newSubtaskInput.classList.add("form-control");
        newSubtaskInput.name = `ToDoItem.SubTasks[${subtaskCount}].Content`;
        newSubtaskInput.placeholder = "Enter subtask";

        const removeBtn = document.createElement("button");
        removeBtn.type = "button";
        removeBtn.classList.add("btn", "btn-danger", "mt-2");
        removeBtn.textContent = "-";
        removeBtn.addEventListener("click", function () {
            subtaskContainer.removeChild(newSubtaskDiv);
            updateSubtaskIndexes();
        });

       /* newSubtaskDiv.appendChild(newSubtaskLabel);*/
        newSubtaskDiv.appendChild(newSubtaskInput);
        newSubtaskDiv.appendChild(removeBtn);
        subtaskContainer.appendChild(newSubtaskDiv);

        subtaskCount++;
        updateSubtaskIndexes();
    });
    updateSubtaskIndexes();
});
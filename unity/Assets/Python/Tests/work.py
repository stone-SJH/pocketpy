import os

def merge_files(directory):
    result_filename = "result.txt"
    result_file = open(result_filename, "w", encoding="utf-8")

    for filename in os.listdir(directory):
        if filename.endswith(".py"):
            file_path = os.path.join(directory, filename)
            with open(file_path, "r", encoding="utf-8") as file:
                result_file.write(file.read())
                result_file.write("\n")

    result_file.close()
    print(f"Merged contents of .py files in {directory} into {result_filename}.")

# Specify the directory path here
directory_path = "./py/"
merge_files(directory_path)
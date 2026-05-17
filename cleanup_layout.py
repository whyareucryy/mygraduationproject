import re

file_path = r'C:\Users\mihai\source\repos\ComputerRepairService\ComputerRepairService\Views\Shared\_Layout.cshtml'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the internal <style> block
pattern = r'<style>.*?</style>'
content = re.sub(pattern, '', content, flags=re.DOTALL)

# Update navbar classes to match new light mode
content = content.replace('navbar-dark bg-primary', 'navbar-light bg-white')
content = content.replace('btn btn-outline-light me-3', 'btn btn-outline-secondary me-3')
content = content.replace('text-light opacity-75', 'text-muted')

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Cleaned up _Layout.cshtml")

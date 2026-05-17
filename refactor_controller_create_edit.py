import re

file_path = r'C:\Users\mihai\source\repos\ComputerRepairService\ComputerRepairService\Controllers\ServiceOrdersController.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace POST Create
post_create_pattern = r'(\[HttpPost\]\s+\[ValidateAntiForgeryToken\]\s+public async Task<IActionResult> Create\(\s*\[Bind\(".*?\"\)\]\s*ServiceOrder serviceOrder,\s*int\[\] selectedTechnicians\)\s+\{)(.*?)(return View\(serviceOrder\);\s+\})'

post_create_new = r'''[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateViewData();
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Client"))
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    if (customer != null) model.CustomerId = customer.CustomerId;
                    model.SelectedTechnicians = Array.Empty<int>();
                }

                var serviceOrder = await _orderService.CreateOrderAsync(model, user.Id, roles);
                TempData["SuccessMessage"] = "Заказ успешно создан!";
                
                if (roles.Contains("Client")) return RedirectToAction(nameof(MyOrders));
                return RedirectToAction(nameof(Details), new { id = serviceOrder.OrderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при создании заказа: {ex.Message}");
                await LoadCreateViewData();
                return View(model);
            }
        }'''

content = re.sub(post_create_pattern, post_create_new, content, flags=re.DOTALL)

# Replace POST Edit
post_edit_pattern = r'(\[Authorize\(Roles = "Admin,Employee"\)\]\s+\[HttpPost\]\s+\[ValidateAntiForgeryToken\]\s+public async Task<IActionResult> Edit\(int id,\s*\[Bind\(".*?\"\)\]\s*ServiceOrder serviceOrder,\s*int\[\] selectedTechnicians\)\s+\{)(.*?)(return View\(serviceOrder\);\s+\})'

post_edit_new = r'''[Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
        {
            if (id != model.OrderId || !ModelState.IsValid)
            {
                await LoadCreateViewData();
                return View(model);
            }

            try
            {
                await _orderService.EditOrderAsync(model, User.Identity?.Name);
                TempData["SuccessMessage"] = "Заказ успешно обновлен!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при обновлении заказа: {ex.Message}");
                await LoadCreateViewData();
                return View(model);
            }
        }'''

content = re.sub(post_edit_pattern, post_edit_new, content, flags=re.DOTALL)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Replaced POST Create and Edit")

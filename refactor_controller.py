import re

file_path = r'C:\Users\mihai\source\repos\ComputerRepairService\ComputerRepairService\Controllers\ServiceOrdersController.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add namespace
content = content.replace('using ComputerRepairService.Services;', 'using ComputerRepairService.Services;\nusing ComputerRepairService.Services.Interfaces;')

# 2. Update constructor
old_constructor = '''        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceOrdersController(RepairDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }'''

new_constructor = '''        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderManagementService _orderService;

        public ServiceOrdersController(RepairDbContext context, UserManager<ApplicationUser> userManager, IOrderManagementService orderService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
        }'''

content = content.replace(old_constructor, new_constructor)

# 3. Replace TakeInWork
take_in_work_pattern = r'(\[Authorize\(Roles = "Admin,Employee"\)\]\s+\[HttpPost\]\s+\[ValidateAntiForgeryToken\]\s+public async Task<IActionResult> TakeInWork\(int id\)\s+\{)(.*?)(return RedirectToAction\(nameof\(ClientRequests\)\);\s+\})'
take_in_work_new = r'''\1
            var user = await _userManager.GetUserAsync(User);
            bool success = await _orderService.TakeOrderInWorkAsync(id, user?.Id, User.Identity?.Name, User.IsInRole("Employee"));
            
            if (success)
            {
                TempData["SuccessMessage"] = $"Заявка #{id} взята в работу.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ошибка при взятии заявки в работу.";
            }
            \3'''
content = re.sub(take_in_work_pattern, take_in_work_new, content, flags=re.DOTALL)

# 4. Replace CancelOrder
cancel_pattern = r'(\[Authorize\(Roles = "Client"\)\]\s+\[HttpPost\]\s+\[ValidateAntiForgeryToken\]\s+public async Task<IActionResult> CancelOrder\(int id\)\s+\{)(.*?)(return RedirectToAction\(nameof\(MyOrders\)\);\s+\})'
cancel_new = r'''\1
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return RedirectToAction("AccessDenied", "Home");

            bool success = await _orderService.CancelOrderAsync(id, customer.CustomerId, User.Identity?.Name);
            if (success) TempData["SuccessMessage"] = "Заказ успешно отменен!";
            else TempData["ErrorMessage"] = "Ошибка при отмене заказа.";
            
            \3'''
content = re.sub(cancel_pattern, cancel_new, content, flags=re.DOTALL)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Replaced TakeInWork and CancelOrder")

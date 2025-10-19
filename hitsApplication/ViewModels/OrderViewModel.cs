using hitsApplication.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace hitsApplication.ViewModels
{
    namespace hitsApplication.ViewModels
    {
        public class OrderViewModel
        {
            [Required(ErrorMessage = "Адрес доставки обязателен")]
            [Display(Name = "Адрес доставки")]
            public string Address { get; set; } = string.Empty;

            [Required(ErrorMessage = "Телефон обязателен")]
            [Display(Name = "Номер телефона")]
            [RegularExpression(@"^(\+7|7|8)?[\s\-]?\(?[0-9]{3}\)?[\s\-]?[0-9]{3}[\s\-]?[0-9]{2}[\s\-]?[0-9]{2}$",
                ErrorMessage = "Введите корректный номер телефона")]
            public string Phone { get; set; } = string.Empty;

            [Display(Name = "Комментарий к заказу")]
            public string Comment { get; set; } = string.Empty;

            [Required(ErrorMessage = "Выберите способ оплаты")]
            [Display(Name = "Способ оплаты")]
            public string PaymentMethod { get; set; } = string.Empty;

            [Display(Name = "Пароль для регистрации")]
            [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
            public string? Password { get; set; }

            public Cart Cart { get; set; } = new Cart();
        }
    }
}

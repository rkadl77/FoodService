using System.ComponentModel.DataAnnotations;

namespace hitsApplication.Models
{
    public class FeatureFlags
    {
        [Display(Name = "Ломать создание заказа")]
        public bool BreakOrderCreation { get; set; } = true;  // Для бага 1

        [Display(Name = "Не изменять количество при добавлении")]
        public bool NoQuantityChangeOnAdd { get; set; } = true;  // Для бага 3

        [Display(Name = "Не изменять количество при удалении")]
        public bool NoQuantityChangeOnRemove { get; set; } = true;  // Для бага 4

        [Display(Name = "Не очищать корзину после заказа")]
        public bool NoCartClearAfterOrder { get; set; } = true;  // Для бага 5

        [Display(Name = "Включить баг с расчетом цены")]
        public bool EnableCalculationBug { get; set; } = false;

        [Display(Name = "Включить баг с переполнением количества")]
        public bool EnableOverflowBug { get; set; } = false;

        [Display(Name = "Включить баг с URL изображений")]
        public bool EnableImageUrlBug { get; set; } = false;

        [Display(Name = "Включить баг в ответе API")]
        public bool EnableResponseBug { get; set; } = false;

        [Display(Name = "Включить баг с логированием")]
        public bool EnableInfoLeakBug { get; set; } = false;

        [Display(Name = "Включить баг с валидацией")]
        public bool EnableValidationBug { get; set; } = false;

        [Display(Name = "Включить новую логику корзины")]
        public bool EnableNewCartLogic { get; set; } = false;

        [Display(Name = "Включить интеграцию с Java")]
        public bool EnableJavaIntegration { get; set; } = true;

        [Display(Name = "Лимит товаров в корзине")]
        public int CartItemLimit { get; set; } = 50;
        public string JavaServiceUrl { get; set; } = "http://localhost:8096";
    }
}
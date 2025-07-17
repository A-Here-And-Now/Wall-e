namespace walle.ServiceLayer{
    public interface IRateService
    {
        decimal GetRate(Currency baseC, Currency origC);
    }
}
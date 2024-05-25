using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculatorCsharp
{
    public interface IOperation
    {
        string Name { get; }
        double Run(params double[] numbers);
    }

    public interface IOperationProvider
    {
        IEnumerable<IOperation> Get();
    }

    public interface IMenu<out T>
    {
        IMenu<T> Show();
        IMenuItemSelector<T> ItemSelector { get; }
    }

    public interface IMenuItemSelector<out T>
    {
        T Select();
    }

    public interface IOperationMenuItemSelector : IMenuItemSelector<IOperation>
    {
    }

    public interface IMenuItemSelectorProvider
    {
        int GetMenuItemId();
    }

    internal class LocalInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IWindsorContainer>().Instance(container),
                Component.For<Application>().StartUsingMethod("Run"),
                Component.For<IOperationMenuItemSelector>().ImplementedBy<OperationMenuItemSelector>().LifestyleTransient(),
                Component.For<IMenuItemSelectorProvider>().ImplementedBy<OperationMenuItemSelectorView>().LifestyleTransient(),
                Component.For<IOperationProvider>().ImplementedBy<OperationProvider>(),
                Component.For<IMenu<IOperation>>().ImplementedBy<OperationMenu>().LifestyleTransient(),

                Component.For<IOperation>().ImplementedBy<Addition>(),
                Component.For<IOperation>().ImplementedBy<Subtraction>(),
                Component.For<IOperation>().ImplementedBy<Multiplication>(),
                Component.For<IOperation>().ImplementedBy<Division>(),
                Component.For<IOperation>().ImplementedBy<Power>(),
                Component.For<IOperation>().ImplementedBy<SquareRoot>(),
                Component.For<IOperation>().ImplementedBy<Sin>(),
                Component.For<IOperation>().ImplementedBy<Cos>(),
                Component.For<IOperation>().ImplementedBy<Tan>(),
                Component.For<IOperation>().ImplementedBy<Cotan>(),
                Component.For<IOperation>().ImplementedBy<NaturalLogarithm>(),
                Component.For<IOperation>().ImplementedBy<DecimalLogarithm>()
            );
        }
    }

    public class Program
    {
        private static IWindsorContainer _container = new WindsorContainer();

        public static void Main()
        {
            try
            {
                Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                _container?.Dispose();
            }
        }

        private static void Start()
        {
            _container.Kernel.AddFacility<StartableFacility>(f => f.DeferredStart());
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel));
            _container.Install(new LocalInstaller());
        }
    }

    public class Application
    {
        private readonly IMenu<IOperation> menu;

        public Application(IMenu<IOperation> menu)
        {
            this.menu = menu;
        }

        public void Run()
        {
            while (true)
            {
                var operation = menu.Show().ItemSelector.Select();
                if (operation == null) break;

                double result;
                try
                {
                    if (operation is Sin || operation is Cos || operation is Tan || operation is Cotan || operation is NaturalLogarithm || operation is DecimalLogarithm)
                    {
                        result = operation.Run(GetInput("Введите число: "));
                    }
                    else
                    {
                        result = operation.Run(GetInput("Введите первое число: "), GetInput("Введите второе число: "));
                    }

                    Console.WriteLine($"Результат: {result}");
                }
                catch (DivideByZeroException ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
                catch (FormatException)
                {
                    Console.WriteLine("Ошибка: Неверный формат числа.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        private double GetInput(string prompt)
        {
            Console.Write(prompt);
            double input;
            while (!double.TryParse(Console.ReadLine(), out input))
            {
                Console.WriteLine("Ошибка: Неверный формат числа. Пожалуйста, введите число снова.");
                Console.Write(prompt);
            }
            return input;
        }
    }

    public sealed class OperationProvider : IOperationProvider
    {
        private readonly IEnumerable<IOperation> operations;

        public OperationProvider(IEnumerable<IOperation> operations)
        {
            this.operations = operations;
        }

        public IEnumerable<IOperation> Get() => operations;
    }

    public sealed class OperationMenu : IMenu<IOperation>
    {
        private readonly IOperationProvider operationProvider;

        public OperationMenu(IOperationProvider operationProvider, IOperationMenuItemSelector menuItemSelector)
        {
            this.operationProvider = operationProvider;
            ItemSelector = menuItemSelector;
        }

        public IMenuItemSelector<IOperation> ItemSelector { get; }

        public IMenu<IOperation> Show()
        {
            Console.WriteLine("======== КАЛЬКУЛЯТОР ==========");
            int i = 1;
            foreach (var operation in operationProvider.Get())
                Console.WriteLine($"{i++}.{operation.Name}");
            return this;
        }
    }

    public sealed class OperationMenuItemSelectorView : IMenuItemSelectorProvider
    {
        public int GetMenuItemId()
        {
            Console.Write("Выберите действие: ");
            return Convert.ToInt32(Console.ReadLine());
        }
    }

    public sealed class OperationMenuItemSelector : IOperationMenuItemSelector
    {
        private readonly IMenuItemSelectorProvider selector;
        private readonly IOperationProvider operationProvider;

        public OperationMenuItemSelector(IMenuItemSelectorProvider selector, IOperationProvider operationProvider)
        {
            this.selector = selector;
            this.operationProvider = operationProvider;
        }

        public IOperation Select()
        {
            int id = selector.GetMenuItemId();
            return id > 0 ? operationProvider.Get().ElementAtOrDefault(id - 1) : null;
        }
    }

    public abstract class Operation : IOperation
    {
        public string Name { get; }

        protected Operation(string name)
        {
            Name = name;
        }

        public abstract double Run(params double[] numbers);
    }

    public sealed class Addition : Operation
    {
        public Addition() : base("Сложение") { }

        public override double Run(params double[] numbers) => numbers.Sum();
    }

    public sealed class Subtraction : Operation
    {
        public Subtraction() : base("Вычитание") { }

        public override double Run(params double[] numbers) => numbers.Aggregate((a, b) => a - b);
    }

    public sealed class Multiplication : Operation
    {
        public Multiplication() : base("Умножение") { }

        public override double Run(params double[] numbers) => numbers.Aggregate((a, b) => a * b);
    }

    public sealed class Division : Operation
    {
        public Division() : base("Деление") { }

        public override double Run(params double[] numbers)
        {
            if (numbers.Length < 2 || numbers[1] == 0)
            {
                throw new DivideByZeroException("Деление на ноль или отсутствие второго числа.");
            }

            return numbers.Aggregate((a, b) => a / b);
        }
    }

    public sealed class Power : Operation
    {
        public Power() : base("Возведение в степень") { }

        public override double Run(params double[] numbers) => Math.Pow(numbers[0], numbers[1]);
    }

    public sealed class SquareRoot : Operation
    {
        public SquareRoot() : base("Квадратный корень") { }

        public override double Run(params double[] numbers) => Math.Sqrt(numbers[0]);
    }

    public sealed class Sin : Operation
    {
        public Sin() : base("Синус") { }

        public override double Run(params double[] numbers) => Math.Sin(numbers[0]);
    }

    public sealed class Cos : Operation
    {
        public Cos() : base("Косинус") { }

        public override double Run(params double[] numbers) => Math.Cos(numbers[0]);
    }

    public sealed class Tan : Operation
    {
        public Tan() : base("Тангенс") { }

        public override double Run(params double[] numbers) => Math.Tan(numbers[0]);
    }

    public sealed class Cotan : Operation
    {
        public Cotan() : base("Котангенс") { }

        public override double Run(params double[] numbers) => 1 / Math.Tan(numbers[0]);
    }

    public sealed class NaturalLogarithm : Operation
    {
        public NaturalLogarithm() : base("Натуральный логарифм (ln)") { }

        public override double Run(params double[] numbers) => Math.Log(numbers[0]);
    }

    public sealed class DecimalLogarithm : Operation
    {
        public DecimalLogarithm() : base("Десятичный логарифм (log10)") { }

        public override double Run(params double[] numbers) => Math.Log10(numbers[0]);
    }
}


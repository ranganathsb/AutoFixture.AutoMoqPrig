# AutoFixture.AutoMoqPrig
[AutoFixture](https://github.com/AutoFixture/AutoFixture) with auto mocking using [Moq](https://github.com/Moq/moq4) and [Prig](https://github.com/urasandesu/Prig).



## INSTALLATION
Install Chocolatey in accordance with [the top page](https://chocolatey.org/). Then, run command prompt as Administrator, execute the following command: 
```dos
CMD C:\> cinst autofixture.automoqprig -y
```

Finally, execute `Install-Package` in the Package Manager Console for your test project: 
```powershell
PM> Install-Package autofixture.automoqprig
```



## USAGE
You can customize AutoFixture with `AutoConfiguredMoqPrigCustomization` to mock any Prig Type automatically: 
```cs
[TestFixture]
public class Class1
{
    [Test]
    public void AutoConfiguredMoqPrigCustomization_can_create_auto_mocked_prig_type()
    {
        using (new IndirectionsContext())
        {
            // Arrange
            var ms = new MockStorage(MockBehavior.Strict);
            var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

            fixture.Create<PProcess>();
            fixture.Create<PProcessModule>();


            // Act
            var result = Process.Start("Foo").MainModule.FileName;


            // Assert
            Assert.That(result, Is.Not.Contains("Foo"));
            // Because `FileName` is generated automatically by AutoFixture. e.g. c0f98d5f-c6f7-48af-ac39-9e8217647cc2
        }
    }
}
```

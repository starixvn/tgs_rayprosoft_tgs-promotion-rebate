using Tgs.Services.Rebate;

namespace RebateService.Core.UnitTests;

public class CycleManagerTests
{
	[Fact]
	public void 週一結算_輸入20221205_觸發日為20221205()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-5");

		var expected = inputDate;

		// Act
		var actual = sut.GetNextOccurrence(inputDate);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 週一結算_輸入20221209_前一個觸發日應為20221205()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-9");

		var expected = DateTime.Parse("2022-12-5");

		// Act
		var actual = sut.GetPreviousOccurrence(inputDate);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 週一結算_輸入20221212_前一個觸發日為20221205()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-12");

		var expected = DateTime.Parse("2022-12-5");

		// Act
		var actual = sut.GetPreviousOccurrence(inputDate);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 週一結算_起始日為當日_當日應為觸發日()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-5"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-5");

		var expected = inputDate;

		// Act
		var actual = sut.GetNextOccurrence(inputDate);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 週一結算_當日為20221209_下一個觸發日應為20221212()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-9");

		var expected = DateTime.Parse("2022-12-12");

		// Act
		var actual = sut.GetNextOccurrence(inputDate);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 週一結算_當日為20221202_應該找不到前一個觸發日()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-2");

		// Act
		var actual = sut.GetPreviousOccurrence(inputDate);

		// Assert
		Assert.Null(actual);
	}

	[Fact]
	public void 週一結算_當日為20221230_應該找不到下一個觸發日()
	{
		// Arrange
		var sut = new CycleManager(
			"0 0 * * Mon",
			DateTime.Parse("2022-12-1"),
			DateTime.Parse("2023-1-1"));

		var inputDate = DateTime.Parse("2022-12-30");

		// Act
		var actual = sut.GetNextOccurrence(inputDate);

		// Assert
		Assert.Null(actual);
	}
}

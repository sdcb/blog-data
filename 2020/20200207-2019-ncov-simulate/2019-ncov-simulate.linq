<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>FlysEngine</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
</Query>

static Random random = new Random();
const float SafeDistance = 2.0f; // 要靠多近，才会触发感染验证
const float InffectRate = 0.8f; // 靠得够近时，被携带者感染的机率
const float SecondsPerDay = 1.0f; // 模拟器的秒数，对应真实一天
const double MovingWilling = 0.99; // 移动意愿，0-1
const float MovingDistancePerDay = 50.0f; // 每天移动距离
const int InitialInfectorCount = 5; // 最初感染者数
const double DeathRate = 0.021; // 死亡率
const int HospitalBeds = 80;

const float PersonSize = 4.0f;
const float HospitalBedSize = 20.0f;
const float HospitalHeight = 800.0f;

// 住院治愈时间，最短5天，最长12.75天，平均约7天
static float GenerateCureDays() => random.NextFloat(5, 12.75f);
static float GenerateShadowDays() => random.Next(1, 14);

void Main()
{
	using (var w = new VirusWindow { Text = "病毒传播模拟", WindowState = FormWindowState.Maximized })
	{
		RenderLoop.Run(w, () => w.Render(1, PresentFlags.None));
	}
}

class VirusWindow : RenderWindow
{
	City[] Cities = new[] { City.Create() };

	protected override void OnUpdateLogic(float dt)
	{
		foreach (var city in Cities)
		{
			city.Update(dt);
		}
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(Color.DarkGray);

		float minEdge = Math.Min(ClientSize.Width / 2, ClientSize.Height / 2);
		float scale = minEdge / 540; // relative coordinate
		ctx.Transform =
			Matrix3x2.Scaling(scale) *
			Matrix3x2.Translation(ClientSize.Width / 2, ClientSize.Height / 2);
		foreach (var city in Cities)
		{
			city.Draw(ctx, XResource);
		}
	}
}

class City
{
	public const int Population = 5000;
	public float CitySize = 400;
	private float day = 1;

	public Person[] Persons;
	private SortedSet<int> infectorIds = new SortedSet<int>();
	private SortedSet<int> healthyIds = new SortedSet<int>();

	public Hospital Hospital = new Hospital();

	public static City Create()
	{
		var c = new City();
		c.Persons = Enumerable.Range(1, Population)
			.Select(x => Person.Create(c.CitySize))
			.ToArray();
		c.healthyIds = new SortedSet<int>(Enumerable
			.Range(0, Population));
		for (var i = 0; i < InitialInfectorCount; ++i) c.Infect(i);
		return c;
	}

	public void Infect(int personId)
	{
		Persons[personId].Status = PersonStatus.InfectedInShadow;
		Persons[personId].EstimateDays = GenerateShadowDays();
		healthyIds.Remove(personId);
		infectorIds.Add(personId);
	}

	internal void Draw(DeviceContext ctx, XResource x)
	{
		ctx.DrawEllipse(
			new Ellipse(new Vector2(0, 0), CitySize, CitySize),
			x.GetColor(Color.Blue),
			2.0f);

		for (var i = 0; i < Persons.Length; ++i)
		{
			Persons[i].Draw(ctx, x);
		}

		Hospital.Draw(ctx, x);
		DrawStatus(ctx, x);
	}

	void DrawStatus(DeviceContext ctx, XResource x)
	{
		ctx.Transform = Matrix3x2.Identity;
		int healthy = 0, infected = 0, illness = 0, inHospital = 0, cured = 0, dead = 0;
		for (var i = 0; i < Persons.Length; ++i)
		{
			_ = Persons[i].Status switch
			{
				PersonStatus.Healthy => ++healthy,
				PersonStatus.InfectedInShadow => ++infected,
				PersonStatus.Illness => ++illness,
				PersonStatus.InHospital => ++inHospital,
				PersonStatus.Cured => ++cured,
				PersonStatus.Dead => ++dead,
				_ => throw new InvalidOperationException()
			};
		}
		var texts = new[]
		{
			(text: $"第{day:F1}天", color: Color.Black),
			(text: $"健康人数：{healthy}", color: ColorFromStatus(PersonStatus.Healthy)),
			(text: $"感染人数：{infected}", color: ColorFromStatus(PersonStatus.InfectedInShadow)),
			(text: $"发病人数：{illness+inHospital}", color: ColorFromStatus(PersonStatus.Illness)),
			(text: $"住院人数：{inHospital}/{HospitalBeds}", color: ColorFromStatus(PersonStatus.InHospital)),
			(text: $"治愈人数：{cured}", color: ColorFromStatus(PersonStatus.Cured)),
			(text: $"死亡人数：{dead}", color: ColorFromStatus(PersonStatus.Dead)),
		};
		for (var i = 0; i < texts.Length; ++i)
		{
			ctx.DrawText(texts[i].text, x.TextFormats[20], new RectangleF(10, i * 24, ctx.Size.Width, ctx.Size.Height), x.GetColor(texts[i].color));
		}
	}

	float dayAccumulate = 0;
	internal void Update(float dt)
	{
		// step move
		for (var i = 0; i < Persons.Length; ++i)
		{
			Persons[i].MoveAroundInCity(dt, CitySize);
		}

		// step status
		dayAccumulate += dt;
		day += (dt / SecondsPerDay);

		while (dayAccumulate >= SecondsPerDay)
		{
			StepDay();
			dayAccumulate -= SecondsPerDay;
		}
	}

	void StepDay()
	{
		Hospital.Heal(Persons);

		// infected -> illness
		for (var i = 0; i < infectorIds.Count; ++i)
		{
			if (Persons[i].Status == PersonStatus.InfectedInShadow && --Persons[i].EstimateDays <= 0)
			{
				Persons[i].Status = PersonStatus.Illness;
			}
		}

		// healthy -> infected
		List<int> newlyInffectedIds = new List<int>();
		newlyInffectedIds = healthyIds
			.AsParallel()
			.Where(x =>
			{
				foreach (var infectorId in infectorIds)
				{
					if (Vector2.DistanceSquared(Persons[x].Position, Persons[infectorId].Position) <= SafeDistance * SafeDistance)
						return true;
				}
				return false;
			})
			.ToList();

		foreach (int personId in newlyInffectedIds)
		{
			Infect(personId);
		}

		for (var i = 0; i < Persons.Length; ++i)
		{
			// infected -> dead
			if ((Persons[i].Status == PersonStatus.Illness || Persons[i].Status == PersonStatus.InHospital)
				&& random.NextDouble() < (DeathRate / 2))
			{
				infectorIds.Remove(i); Hospital.PersonIds.Remove(i);
				Persons[i].Status = PersonStatus.Dead;
				Persons[i].Position = new Vector2(int.MaxValue, int.MaxValue);
			}

			// illness -> inHospital
			if (Hospital.HasBed && Persons[i].Status == PersonStatus.Illness)
			{
				Hospital.Accept(Persons, i);
			}
		}
	}
}

class Hospital
{
	public int Beds = HospitalBeds;
	public SortedSet<int> PersonIds = new SortedSet<int>();

	public bool HasBed => Beds > PersonIds.Count;

	public void Heal(Person[] persons)
	{
		var curedIds = new List<int>();
		int index = 0;
		foreach (var i in PersonIds)
		{
			persons[i].Position = GetPosition(index++) + new Vector2(HospitalBedSize / 2 - PersonSize / 2, HospitalBedSize / 2 - PersonSize / 2);

			persons[i].EstimateDays--;
			if (persons[i].EstimateDays <= 0)
			{
				curedIds.Add(i);
			}
		}

		foreach (var id in curedIds)
		{
			persons[id].Status = PersonStatus.Cured;
			persons[id].Position = new Vector2(0, 0);
			PersonIds.Remove(id);
		}
	}

	const float Top = -400; const float Left = 450;
	Vector2 GetPosition(int index)
	{
		int columnBeds = (int)(HospitalHeight / HospitalBedSize);
		int column = index % columnBeds;
		int row = index / columnBeds;
		return new Vector2(Left + row * HospitalBedSize, Top + column * HospitalBedSize);
	}

	public void Accept(Person[] persons, int personId)
	{
		persons[personId].Status = PersonStatus.InHospital;
		persons[personId].EstimateDays = GenerateCureDays();
		PersonIds.Add(personId);
	}

	internal void Draw(DeviceContext ctx, XResource x)
	{
		int rows = (int)MathF.Ceiling(Beds * HospitalBedSize / HospitalHeight);
		float width = rows * HospitalBedSize;

		for (var i = 0; i < Beds; ++i)
		{
			Vector2 topLeft = GetPosition(i);
			ctx.DrawRectangle(new RectangleF(topLeft.X, topLeft.Y, HospitalBedSize, HospitalBedSize), x.GetColor(Color.Green));
		}
		ctx.DrawRectangle(new RectangleF(Left, Top, width, HospitalHeight), x.GetColor(HasBed ? Color.Black : Color.Red), 3.0f);
	}
}

struct Person
{
	public PersonStatus Status;
	public Vector2 Position;
	public float EstimateDays;

	public static Person Create(float citySize)
	{
		float pi = random.NextFloat(0, MathUtil.TwoPi);
		float r = random.NextFloat(0, citySize);
		var p = new Person { Status = PersonStatus.Healthy };
		p.Position.X = (float)Math.Sin(pi) * r;
		p.Position.Y = -(float)Math.Cos(pi) * r;
		return p;
	}

	internal void Draw(DeviceContext ctx, XResource x)
	{
		ctx.FillRectangle(new RectangleF(Position.X, Position.Y, PersonSize, PersonSize), x.GetColor(ColorFromStatus(Status)));
	}

	public void MoveAroundInCity(float dt, float citySize)
	{
		if (Status == PersonStatus.InHospital ||
			Status == PersonStatus.Dead) return;
		if (random.NextDouble() > MovingWilling) return;

		float duration = dt / SecondsPerDay;

		float direction = random.NextFloat(0, MathF.PI * 2);
		float dx = MovingDistancePerDay * duration * MathF.Sin(direction);
		float dy = MovingDistancePerDay * duration * -MathF.Cos(direction);

		var newPosition = Position + new Vector2(dx, dy);
		if (newPosition.LengthSquared() < (citySize * citySize))
		{
			Position = newPosition;
		}
	}
}

enum PersonStatus
{
	Healthy,
	InfectedInShadow,
	Illness,
	InHospital,
	Cured,
	Dead,
}

static Color ColorFromStatus(PersonStatus status) => status switch
{
	PersonStatus.Healthy => Color.Green,
	PersonStatus.InfectedInShadow => Color.Yellow,
	PersonStatus.Illness => Color.OrangeRed,
	PersonStatus.InHospital => Color.Red,
	PersonStatus.Dead => Color.Black,
	PersonStatus.Cured => Color.White,
	_ => throw new InvalidOperationException(),
};
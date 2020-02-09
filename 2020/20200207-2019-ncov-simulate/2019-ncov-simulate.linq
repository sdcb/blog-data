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
static double MoveWilling = 0.90f; // 移动意愿，0-1
static bool WearMask = false; // 是否戴口罩
static int HospitalBeds = 40; // 床位数

const float InffectRate = 0.8f; // 靠得够近时，被携带者感染的机率
const float SecondsPerDay = 0.3f; // 模拟器的秒数，对应真实一天
const float MovingDistancePerDay = 10.0f; // 每天移动距离
const int InitialInfectorCount = 5; // 最初感染者数
const double DeathRate = 0.021; // 死亡率5

// 要靠多近，才会触发感染验证
static float SafeDistance() => WearMask ? 1.5f : 3.5f;

// 住院治愈时间，最短5天，最长12.75天，平均约7天
static float GenerateCureDays() => random.NextFloat(5, 12.75f);
// 潜伏期，1-14天
static float GenerateShadowDays() => random.Next(1, 14);
// 发病后，就医时间，0-3天
static float GenerateToHospitalDays() => random.Next(0, 3);

// 显示参数
const float PersonSize = 4.0f;
const float HospitalBedSize = 20.0f;
const float HospitalHeight = 800.0f;
const float HospitalY = -400; const float HospitalX = 410;

void Main()
{
	Util.NewProcess = true;
	using (var w = new VirusWindow 
	{ 
		Text = "病毒传播模拟", 
		ClientSize = new System.Drawing.Size(600, 600), 
		StartPosition = FormStartPosition.CenterScreen 
	})
	{
		RenderLoop.Run(w, () => w.Render(1, PresentFlags.None));
	}
}

class VirusWindow : RenderWindow
{
	City City = City.Create();

	protected override void OnUpdateLogic(float dt)
	{
		City.Update(dt);
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		switch (e.KeyChar)
		{
			case '1': MoveWilling = 0.10f; break;
			case '2': MoveWilling = 0.50f; break;
			case '3': MoveWilling = 0.90f; break;
			case 'M': WearMask = !WearMask; break;
			case 'A': HospitalBeds += 40; break;
			case 'D': HospitalBeds -= 40; break;
			case 'R':
				{
					if (MessageBox.Show("要重来吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
					{
						City = City.Create();
					}
					break;
				}
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
		City.Draw(ctx, XResource);
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

	float tagDay = 0;
	string notificationText = null, notificationTitle = null;
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

		if (notificationText != null)
		{
			MessageBox.Show(notificationText, notificationTitle);
			notificationText = null;
		}
		if (infected == 0 && illness == 0 && inHospital == 0 && tagDay == 0)
		{
			tagDay = day;
			notificationText = $"你在第{day:F1}天击败了病毒！死亡人数：{dead}";
			notificationTitle = "恭喜！";
		}
		else if (healthy <= (Population / 2) && tagDay == 0)
		{
			tagDay = day;
			notificationText = $"第{day:F1}天，疫情控制失败！\n（超过一半的人被感染即视为失败）";
			notificationTitle = "失败！你没能阻止病毒的肆虐。";
		}

		string wearMaskText = WearMask ? "✔" : "❌";
		var texts = new[]
		{
			(text: $"第{day:F1}天 移动意愿:{MoveWilling:P0} 居民戴口罩:{wearMaskText}", color: Color.Black),
			(text: $"健康人数：{healthy}", color: ColorFromStatus(PersonStatus.Healthy)),
			(text: $"感染人数：{infected}", color: ColorFromStatus(PersonStatus.InfectedInShadow)),
			(text: $"发病人数：{illness+inHospital}", color: ColorFromStatus(PersonStatus.Illness)),
			(text: $"住院人数：{inHospital}/{HospitalBeds}", color: ColorFromStatus(PersonStatus.InHospital)),
			(text: $"治愈人数：{cured}", color: ColorFromStatus(PersonStatus.Cured)),
			(text: $"死亡人数：{dead}", color: ColorFromStatus(PersonStatus.Dead)),
		};
		for (var i = 0; i < texts.Length; ++i)
		{
			ctx.DrawText(texts[i].text, x.TextFormats[18], new RectangleF(5, i * 20, ctx.Size.Width, ctx.Size.Height),
				x.GetColor(texts[i].color),
				DrawTextOptions.EnableColorFont);
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
		Hospital.Heal(Persons, infectorIds);

		for (var i = 0; i < Persons.Length; ++i)
		{
			Persons[i].Direction = random.NextDouble() < MoveWilling ? 
				random.NextFloat(0, MathF.PI * 2) : float.NaN;
			
			// illness/inHospital -> dead
			if ((Persons[i].Status == PersonStatus.Illness || Persons[i].Status == PersonStatus.InHospital)
				&& random.NextDouble() < (DeathRate / 3))
			{
				infectorIds.Remove(i); Hospital.PersonIds.Remove(i);
				if (Persons[i].Status == PersonStatus.InHospital)
				{
					Persons[i].Position = new Vector2(int.MaxValue, int.MaxValue);
				}
				Persons[i].Status = PersonStatus.Dead;
				continue;
			}

			// illness -> inHospital
			if (Persons[i].Status == PersonStatus.Illness)
			{
				--Persons[i].EstimateDays;
				if (Persons[i].EstimateDays <= 0 && Hospital.HasBed)
				{
					Hospital.Accept(Persons, i);
				}

				continue;
			}

			// infected -> illness
			if (Persons[i].Status == PersonStatus.InfectedInShadow)
			{
				--Persons[i].EstimateDays;
				if (Persons[i].EstimateDays <= 0)
				{
					Persons[i].Status = PersonStatus.Illness;
					Persons[i].EstimateDays = GenerateToHospitalDays();
				}
				continue;
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
					if (Vector2.DistanceSquared(Persons[x].Position, Persons[infectorId].Position) <= SafeDistance() * SafeDistance())
						return true;
				}
				return false;
			})
			.ToList();

		foreach (int personId in newlyInffectedIds)
		{
			Infect(personId);
		}
	}
}

class Hospital
{
	public int Beds => HospitalBeds;
	public SortedSet<int> PersonIds = new SortedSet<int>();

	public bool HasBed => Beds > PersonIds.Count;

	public void Heal(Person[] persons, SortedSet<int> infectorIds)
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
				infectorIds.Remove(i);
			}
		}

		foreach (var id in curedIds)
		{
			persons[id].Status = PersonStatus.Cured;
			persons[id].Position = new Vector2(0, 0);
			PersonIds.Remove(id);
		}
	}

	Vector2 GetPosition(int index)
	{
		int columnBeds = (int)(HospitalHeight / HospitalBedSize);
		int column = index % columnBeds;
		int row = index / columnBeds;
		return new Vector2(HospitalX + row * HospitalBedSize, HospitalY + column * HospitalBedSize);
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
		ctx.DrawRectangle(new RectangleF(HospitalX, HospitalY, width, HospitalHeight), x.GetColor(HasBed ? Color.Black : Color.Red), 3.0f);
	}
}

struct Person
{
	public PersonStatus Status;
	public Vector2 Position;
	public float EstimateDays;
	public float Direction;

	public static Person Create(float citySize)
	{
		float pi = random.NextFloat(0, MathUtil.TwoPi);
		float r = random.NextFloat(0, citySize);
		var p = new Person { Status = PersonStatus.Healthy };
		p.Position.X = (float)Math.Sin(pi) * r;
		p.Position.Y = -(float)Math.Cos(pi) * r;
		p.Direction = random.NextFloat(0, MathF.PI * 2);
		return p;
	}

	internal void Draw(DeviceContext ctx, XResource x)
	{
		ctx.FillRectangle(new RectangleF(Position.X, Position.Y, PersonSize, PersonSize), x.GetColor(ColorFromStatus(Status)));
	}

	public void MoveAroundInCity(float dt, float citySize)
	{
		if (Status == PersonStatus.InHospital ||
			Status == PersonStatus.Dead ||
			float.IsNaN(Direction)) return;

		float duration = dt / SecondsPerDay;

		float dx = MovingDistancePerDay * duration * MathF.Sin(Direction);
		float dy = MovingDistancePerDay * duration * -MathF.Cos(Direction);

		var newPosition = Position + new Vector2(dx, dy);
		if (newPosition.LengthSquared() < (citySize * citySize))
		{
			Position = newPosition;
		}
		else
		{
			Direction = random.NextFloat(0, MathF.PI * 2);
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
namespace RpgApi.Models;

public class Shockwave : Skill
{
    public Shockwave() : base()
    {
        Name = "Shockwave";
        ManaCost = 5;
        StaminaCost = 22;
        Cooldown = 3;
        RequiredLevel = 4;
        RequiredStrength = 14;
        RequiredEndurance = 13;
        Description = "Slam the ground, creating a shockwave that knocks back and damages enemies.";
        Type = SkillType.Active;
        AttackPower = 65;
        DefensePower = 0;
        SpeedModifier = 2;
        MagicPower = 10;
    }
}

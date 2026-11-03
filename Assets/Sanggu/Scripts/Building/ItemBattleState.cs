using System;
using System.Collections.Generic;

/// <summary>
/// 전투 한 번 동안의 계약서(23) 상태. 다른 아이템 효과는 BellkeeperTrial이 처리한다.
/// 계약서: 플레이어 HP가 0이 되면 한 번 HP 10으로 버틴다.
/// </summary>
public sealed class ItemBattleState
{
    private readonly HashSet<string> equipped;
    private bool contractUsed;

    public ItemBattleState(IEnumerable<string> ids)
    {
        equipped = new HashSet<string>(ids ?? Array.Empty<string>());
    }

    public int ResolveHp(int hp, int maxHp)
    {
        if (hp <= 0 && equipped.Contains("23") && !contractUsed)
        {
            contractUsed = true;
            hp = 10;
        }
        return Math.Max(0, Math.Min(maxHp, hp));
    }
}

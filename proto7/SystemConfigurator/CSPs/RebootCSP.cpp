#include "stdafx.h"
#include "RebootCSP.h"
#include "MdmProvision.h"
#include "..\SharedUtilities\DMException.h"
#include "..\SharedUtilities\Logger.h"

using namespace std;

// Reboot CSP docs
// https://msdn.microsoft.com/en-us/library/windows/hardware/mt720802(v=vs.85).aspx
//

const wchar_t* IoTDMRegistryRoot = L"Software\\Microsoft\\IoTDM";
const wchar_t* IoTDMRegistryLastRebootCmd = L"LastRebootCmd";

wstring RebootCSP::_lastRebootTime;

RebootCSP gRebootCSP;

RebootCSP::RebootCSP()
{
    TRACE(__FUNCTION__);

    _lastRebootTime = Utils::GetCurrentDateTimeString();
}

void RebootCSP::ExecRebootNow()
{
    TRACE(__FUNCTION__);

    Utils::WriteRegistryValue(IoTDMRegistryRoot, IoTDMRegistryLastRebootCmd, Utils::GetCurrentDateTimeString());

    TRACE(L"\n---- Run Reboot Now\n");
    MdmProvision::RunExec(L"./Device/Vendor/MSFT/Reboot/RebootNow");
}

wstring RebootCSP::GetLastRebootCmdTime()
{
    TRACE(__FUNCTION__);

    wstring lastRebootCmdTime = L"";
    try
    {
        Utils::ReadRegistryValue(IoTDMRegistryRoot, IoTDMRegistryLastRebootCmd);
    }
    catch (DMException&)
    {
        // This is okay to ignore since this device might have never received a reboot command through DM.
    }
    return lastRebootCmdTime;
}

std::wstring RebootCSP::GetLastRebootTime()
{
    TRACE(__FUNCTION__);

    return _lastRebootTime;
}

wstring RebootCSP::GetSingleScheduleTime()
{
    TRACE(L"\n---- Get Single Schedule Time\n");
    wstring time = MdmProvision::RunGetString(L"./Device/Vendor/MSFT/Reboot/Schedule/Single");
    TRACEP(L"    :", time.c_str());
    return time;
}

void RebootCSP::SetSingleScheduleTime(const wstring& dailyScheduleTime)
{
    TRACE(L"\n---- Set Single Schedule Time\n");
    MdmProvision::RunSet(L"./Device/Vendor/MSFT/Reboot/Schedule/Single", dailyScheduleTime);
    TRACEP(L"    :", dailyScheduleTime.c_str());
}

wstring RebootCSP::GetDailyScheduleTime()
{
    TRACE(L"\n---- Get Daily Schedule Time\n");
    wstring time = MdmProvision::RunGetString(L"./Device/Vendor/MSFT/Reboot/Schedule/DailyRecurrent");
    TRACEP(L"    :", time.c_str());
    return time;
}

void RebootCSP::SetDailyScheduleTime(const wstring& dailyScheduleTime)
{
    TRACE(L"\n---- Set Daily Schedule Time\n");
    MdmProvision::RunSet(L"./Device/Vendor/MSFT/Reboot/Schedule/DailyRecurrent", dailyScheduleTime);
    TRACEP(L"    :", dailyScheduleTime.c_str());
}

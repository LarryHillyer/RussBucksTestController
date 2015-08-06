Imports System
Imports System.ComponentModel
Imports System.Math
Imports System.Data
Imports System.Linq
Imports System.Xml.Linq
Imports System.Globalization

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Path

Imports System.Threading
Imports System.Threading.Tasks

Imports WpfApplication1.JoinPools.Models
Imports WpfApplication1.LosersPool.Models

Class MainWindow
    Inherits Window

    Private Sports As New Dictionary(Of String, String)
    Private Property CronJobName As String

    Public Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        Dim currentDateTime As New Date
        Dim cDT As String
        Dim sp As String
        Dim currentDate As String
        Dim currentTime As String

        Dim newDateTime As New Date
        Dim nDT As String
        Dim newDate As String
        Dim newTime As String

        Dim myUpdate As XDocument
        Dim mySchedule As XDocument

        Dim _dbApp As New ApplicationDbContext
        Dim _dbPools As New PoolDbContext
        Dim _dbLoserPool As New LosersPoolContext

        Dim startSeasonDate As New DateTime

        Using (_dbApp)
            Using (_dbPools)
                Using (_dbLoserPool)


                    Dim queryParameters = (From param1 In _dbApp.AppFolders).Single

                    System.IO.Directory.SetCurrentDirectory(queryParameters.testCronJobFolder)
                    Dim rootFolder = queryParameters.testCronJobFolder

                    Dim cronJobName = My.Application.CronJobName1

                    DeleteData(cronJobName)

                    MW1.Title = "Test Cron Job" + cronJobName

                    Dim queryCronJob = (From cronJob2 In _dbApp.CronJobs
                                        Where cronJob2.CronJobName = cronJobName).Single

                    Dim queryCronJobPools = (From cronJobpool1 In _dbApp.CronJobPools
                                             Where cronJobpool1.CronJobName = cronJobName).ToList

                    Dim queryPoolParams3 = (From poolParam3 In _dbPools.PoolParameters
                                            Where poolParam3.CronJob = queryCronJob.CronJobName).ToList

                    If queryCronJob.SelectedSport = "baseball" Then
                        Sports.Add("baseball", "baseball")
                    ElseIf queryCronJob.SelectedSport = "football" Then
                        Sports.Add("football", "football")
                    End If

                    For Each sport1 In Sports

                        For Each pool1 In queryCronJobPools

                            Dim userChoices1 = New UserChoiceList(".\TestDriverFiles\UserChoicesList" + sport1.Value + ".xml", rootFolder, cronJobName, pool1.CronJobPoolAlias)

                            Dim queryTest = (From test1 In _dbPools.Tests
                                             Where test1.Sport = sport1.Value).ToList

                            If queryTest.Count > 0 Then
                                For Each test1 In queryTest
                                    _dbPools.Tests.Remove(test1)
                                Next
                            End If

                            Dim test2 As New Test
                            test2.TestRun = True
                            test2.Sport = sport1.Value
                            test2.PoolAlias = pool1.CronJobPoolAlias
                            test2.CronJob = cronJobName
                            _dbPools.Tests.Add(test2)

                            _dbPools.SaveChanges()

                        Next
                    Next

                    Dim queryPoolParams1 = (From poolParm1 In _dbPools.PoolParameters
                                            Where poolParm1.CronJob = cronJobName).ToList

                    Dim t1 = 3          'Time for making 1st pick in minutes before 1st games 
                    Dim t2 = 3          'Time for making all other picks before games start 
                    Dim t3 = 1          'Scoring Update fcrequency

                    currentDateTime = DateTime.Now
                    cDT = currentDateTime.ToString("g")

                    sp = cDT.IndexOf(" ")

                    currentDate = cDT.Substring(0, sp)
                    currentTime = cDT.Substring(sp + 1)

                    ' Create Schedule Files for 3 time periods using new times

                    For Each pool1 In queryCronJobPools

                        For timePeriodCnt = 1 To 3

                            newDateTime = currentDateTime.AddMinutes(t1 * timePeriodCnt + (timePeriodCnt - 1) * t3 * 3)

                            nDT = newDateTime.ToString("g")

                            sp = nDT.IndexOf(" ")

                            newDate = nDT.Substring(0, sp)
                            newTime = nDT.Substring(sp + 1)

                            For Each sport1 In Sports

                                Dim queryPoolParam1 = (From poolParam1 In _dbPools.PoolParameters
                                                       Where poolParam1.CronJob = cronJobName).ToList

                                Dim cnt As String = CStr(1 + (timePeriodCnt - 1) * (CInt(queryPoolParam1(0).timePeriodIncrement)))

                                mySchedule = XDocument.Load(".\TestDriverFiles\" + "ScheduleFile" + cnt + sport1.Value + ".xml")

                                For Each game In mySchedule.Descendants("schedule").Descendants("TimeP").Descendants("game")

                                    game.Element("gameDate").Value = newDate
                                    game.Element("gameTime").Value = newTime

                                Next

                                mySchedule.Save(".\TestDriverFiles\" + "ScheduleFile" + cnt + sport1.Value + ".xml")

                                Thread.Sleep(2000)

                                ReadScheduleFile.ReadScheduleXMLFileAndWriteToScheduleEntities(".\TestDriverFiles\" + "ScheduleFile" + cnt + sport1.Value + ".xml", cronJobName)

                            Next
                        Next
                    Next

                    For Each pool1 In queryCronJobPools
                        For Each sport1 In Sports
                            Dim queryPoolParams = (From poolParam1 In _dbPools.PoolParameters
                                                   Where poolParam1.CronJob = cronJobName And poolParam1.poolAlias = pool1.CronJobPoolAlias).Single

                            ' Find Season End
                            Dim querySchedule = (From game1 In _dbLoserPool.ScheduleEntities
                                                 Where game1.CronJob = cronJobName).ToList

                            Dim maxTimePeriod = 1
                            For Each game1 In querySchedule
                                Dim TimePeriod = CInt(Mid(game1.TimePeriod, Len(game1.TimePeriod)))
                                If TimePeriod > maxTimePeriod Then
                                    maxTimePeriod = TimePeriod
                                End If
                            Next

                            queryPoolParams.maxTimePeriod = maxTimePeriod

                            Dim querySchedule1 = (From game1 In _dbLoserPool.ScheduleEntities
                                                  Where game1.TimePeriod = queryPoolParams.timePeriodName + CStr(maxTimePeriod) And game1.CronJob = cronJobName).ToList

                            Dim endSeason = DateTime.Parse(querySchedule1(0).StartDate)
                            For Each game1 In querySchedule1
                                Dim endSeasonDateTime = DateTime.Parse(game1.StartDate)
                                If endSeasonDateTime > endSeason Then
                                    endSeason = endSeasonDateTime
                                End If
                            Next

                            Dim querySchedule2 = (From game2 In _dbLoserPool.ScheduleEntities
                                                  Where game2.TimePeriod = queryPoolParams.timePeriodName + "1" And game2.CronJob = cronJobName).ToList

                            startSeasonDate = DateTime.Parse(querySchedule2(0).StartDate)
                            For Each game2 In querySchedule2
                                Dim startSeasonDateTime = DateTime.Parse(game2.StartDate)
                                If startSeasonDateTime < startSeasonDate Then
                                    startSeasonDate = startSeasonDateTime
                                End If
                            Next


                            queryPoolParams.TimePeriod = queryPoolParams.timePeriodName + "1"
                            queryPoolParams.seasonEndDate = CStr(endSeason)
                            queryPoolParams.seasonGameStart = CStr(startSeasonDate)
                            queryPoolParams.seasonStartTime = "12:01 AM"
                            queryPoolParams.poolState = "Enter Picks"

                            _dbPools.SaveChanges()

                        Next
                    Next

                    For Each sport1 In Sports

                        Dim scheduleTimePeriod1 = New CreateSchedulePeriod(sport1.Value, queryPoolParams1(0).timePeriodName, _
                                                   "12:01 AM", startSeasonDate, cronJobName)
                    Next

                    System.IO.Directory.SetCurrentDirectory(queryParameters.testCronJobFolder)
                    rootFolder = queryParameters.testCronJobFolder

                    For timePeriodCnt = 1 To 3

                        newDateTime = currentDateTime.AddMinutes(t1 * timePeriodCnt + (timePeriodCnt - 1) * t3 * 3)
                        nDT = newDateTime.ToString("g")
                        sp = nDT.IndexOf(" ")

                        newDate = nDT.Substring(0, sp)
                        newTime = nDT.Substring(sp + 1)

                        For scoringUpdateCnt = 0 To 3

                            Dim newDateTime1 = newDateTime.AddMinutes(scoringUpdateCnt * t3)

                            nDT = newDateTime1.ToString("g")

                            sp = nDT.IndexOf(" ")

                            Dim newDate1 = nDT.Substring(0, sp)
                            Dim newTime1 = nDT.Substring(sp + 1)


                            For Each sport1 In Sports

                                Dim queryPoolParam1 = (From poolParam1 In _dbPools.PoolParameters
                                                        Where poolParam1.CronJob = cronJobName).ToList

                                Dim cnt As String = CStr(1 + (timePeriodCnt - 1) * CInt(queryPoolParam1(0).timePeriodIncrement))

                                Dim fileNum As String = cnt + "_" + CStr(scoringUpdateCnt)

                                myUpdate = XDocument.Load(".\TestDriverFiles\" + "scoringUpdate" + fileNum + sport1.Value + ".xml")

                                myUpdate.Element("score").Attribute("filetime").Value = newTime1
                                myUpdate.Element("score").Attribute("filedate").Value = newDate1

                                Dim queryUpdateGame = (From game1 In myUpdate.Element("score").Elements("game")).ToList

                                For Each game1 In queryUpdateGame
                                    game1.Element("gamedate").Value = newDate
                                    game1.Element("gametime").Value = newTime
                                Next

                                If myUpdate.Element("score").Attribute("TimePeriod").Value = "Day3" Then

                                    Dim queryUpdateGame1 = (From game1 In myUpdate.Element("score").Elements("game")
                                                            Where game1.Attribute("gamecode").Value = "102").Single

                                    queryUpdateGame1.Element("gamedate").Value = newDate
                                    queryUpdateGame1.Element("gametime").Value = newTime


                                    queryUpdateGame1 = (From game1 In myUpdate.Element("score").Elements("game")
                                                           Where game1.Attribute("gamecode").Value = "305").Single

                                    Dim newDateTime2 = newDateTime.AddMinutes(2)

                                    Dim nDT2 = newDateTime2.ToString("g")
                                    sp = nDT2.IndexOf(" ")

                                    Dim newDate2 = nDT2.Substring(0, sp)
                                    Dim newTime2 = nDT2.Substring(sp + 1)

                                    queryUpdateGame1.Element("gamedate").Value = newDate2
                                    queryUpdateGame1.Element("gametime").Value = newTime2

                                End If

                                myUpdate.Save(".\TestDriverFiles\" + "scoringUpdate" + fileNum + sport1.Value + ".xml")

                            Next
                        Next
                    Next

                    Dim sT As New SleepyThread(Sports)

                    sT.TD1 = t1
                    sT.TD2 = t2
                    sT.TD3 = t3
                    sT.cronJobName = cronJobName


                    Dim updateFileThread As New Thread(AddressOf sT.ToSleep)
                    updateFileThread.IsBackground = True
                    updateFileThread.Start()

                End Using
            End Using
        End Using

    End Sub

    Private Sub DeleteData(cronJobName As String)



        Dim _dbLoserPool4 = New LosersPoolContext
        Try
            Using (_dbLoserPool4)

                Dim queryUsersChoices = (From user1 In _dbLoserPool4.UserChoicesList
                                         Where user1.CronJob = cronJobName).ToList

                If queryUsersChoices.Count > 0 Then
                    For Each user1 In queryUsersChoices
                        _dbLoserPool4.UserChoicesList.Remove(user1)
                    Next
                End If

                Dim queryTimePeriods = (From user1 In _dbLoserPool4.ScheduleTimePeriods
                                        Where user1.CronJob = cronJobName).ToList

                If queryTimePeriods.Count > 0 Then
                    For Each timeperiod1 In queryTimePeriods
                        _dbLoserPool4.ScheduleTimePeriods.Remove(timeperiod1)
                    Next
                End If

                Dim querySchedule = (From game1 In _dbLoserPool4.ScheduleEntities
                                     Where game1.CronJob = cronJobName).ToList

                If querySchedule.Count > 0 Then
                    For Each game1 In querySchedule
                        _dbLoserPool4.ScheduleEntities.Remove(game1)
                    Next
                End If

                Dim queryLosers = (From game1 In _dbLoserPool4.LoserList
                                   Where game1.CronJob = cronJobName).ToList

                If queryLosers.Count > 0 Then
                    For Each loser1 In queryLosers
                        _dbLoserPool4.LoserList.Remove(loser1)
                    Next
                End If

                Dim queryByeTeams = (From game1 In _dbLoserPool4.ByeTeamsList
                                     Where game1.CronJob = cronJobName).ToList

                If queryByeTeams.Count > 0 Then
                    For Each byeteam1 In queryByeTeams
                        _dbLoserPool4.ByeTeamsList.Remove(byeteam1)
                    Next
                End If

                Dim queryCurrentScoringUpdate = (From game1 In _dbLoserPool4.CurrentScoringUpdates
                                                 Where game1.CronJobName = cronJobName).ToList

                If queryCurrentScoringUpdate.Count > 0 Then
                    For Each cSU1 In queryCurrentScoringUpdate
                        _dbLoserPool4.CurrentScoringUpdates.Remove(cSU1)
                    Next
                End If

                Dim queryScoringUpdate = (From game1 In _dbLoserPool4.ScoringUpdates
                                          Where game1.CronJobName = cronJobName).ToList

                If queryScoringUpdate.Count > 0 Then
                    For Each sU1 In queryScoringUpdate
                        _dbLoserPool4.ScoringUpdates.Remove(sU1)
                    Next
                End If

                Dim queryPostponedGames = (From game1 In _dbLoserPool4.PostponedGames
                                           Where game1.CronJobName = cronJobName).ToList

                If queryPostponedGames.Count > 0 Then
                    For Each game1 In queryPostponedGames
                        _dbLoserPool4.PostponedGames.Remove(game1)
                    Next
                End If

                Dim queryDeletedGames = (From game1 In _dbLoserPool4.DeletedGames
                           Where game1.CronJob = cronJobName).ToList

                If queryDeletedGames.Count > 0 Then
                    For Each game1 In queryDeletedGames
                        _dbLoserPool4.DeletedGames.Remove(game1)
                    Next
                End If

                Dim queryUserPicks = (From qUP1 In _dbLoserPool4.UserPicks
                                      Where qUP1.CronJobName = cronJobName).ToList

                If queryUserPicks.Count > 0 Then
                    For Each game1 In queryUserPicks
                        _dbLoserPool4.UserPicks.Remove(game1)
                    Next
                End If

                _dbLoserPool4.SaveChanges()


            End Using
        Catch ex As Exception

        End Try

    End Sub

    Private Sub MW1_Closing(sender As Object, e As CancelEventArgs)

    End Sub
End Class

Public Class SleepyThread

    Public Property TD1 As Int32
    Public Property TD2 As Int32
    Public Property TD3 As Int32

    Public Property cronJobName As String


    Private Sports1 As New Dictionary(Of String, String)
    Public Sub New(Sports As Dictionary(Of String, String))
        For Each sport1 In Sports
            Sports1.Add(sport1.Key, sport1.Value)
        Next
    End Sub

    Public Sub ToSleep()

        Dim queryGame As New List(Of ScoringUpdate)
        Dim myUpdate As New XDocument
        Dim allGamesAreFinal As Boolean

        Dim _dbApp5 As New ApplicationDbContext
        Dim _dbPools5 As New PoolDbContext
        Dim _dbLoserPool5 As New LosersPoolContext

        Try
            Using (_dbApp5)
                Using (_dbPools5)
                    Using (_dbLoserPool5)
                        Dim SeasonHasEnded As Boolean = False

                        Thread.Sleep(TimeSpan.FromMinutes(Me.TD1))

                        Dim queryCronJobPools = (From pool1 In _dbApp5.CronJobPools
                                                 Where pool1.CronJobName = Me.cronJobName).ToList

                        Dim thisTimePeriod As String
                        For timePeriodCnt = 1 To 3

                            Dim queryPoolParam3 = (From poolParam1 In _dbPools5.PoolParameters
                                           Where poolParam1.CronJob = Me.cronJobName).ToList

                            Dim cnt As String

                            For Each pool1 In queryPoolParam3
                                pool1.poolState = "Scoring Update"

                                cnt = CStr(1 + (timePeriodCnt - 1) * CInt(pool1.timePeriodIncrement))
                                thisTimePeriod = pool1.timePeriodName + cnt

                                pool1.TimePeriod = thisTimePeriod

                                _dbPools5.SaveChanges()
                            Next

                            For Each pool1 In queryCronJobPools
                                Dim queryPoolParam4 = (From qPP4 In _dbPools5.PoolParameters
                                                       Where qPP4.poolAlias = pool1.CronJobPoolAlias).Single

                                For Each sport1 In Sports1
                                    ContenderStatus.RealLosers(queryPoolParam4.TimePeriod, pool1.CronJobPoolAlias, sport1.Value, Me.cronJobName)
                                Next
                            Next

                            For scoringUpdateCnt = 0 To 3
                                For Each sport1 In Sports1

                                    Dim teams1 = (From teams2 In _dbPools5.Teams
                                                Where teams2.Sport = sport1.Value And teams2.TeamName <> "dummy").ToList

                                    Dim queryPoolParam1 = (From poolParam1 In _dbPools5.PoolParameters
                                                           Where poolParam1.CronJob = Me.cronJobName).ToList

                                    cnt = CStr(1 + (timePeriodCnt - 1) * CInt(queryPoolParam1(0).timePeriodIncrement))
                                    thisTimePeriod = queryPoolParam1(0).timePeriodName + cnt
                                    Dim fileNum As String = cnt + "_" + CStr(scoringUpdateCnt)
                                    myUpdate = XDocument.Load(".\TestDriverFiles\" + "scoringUpdate" + fileNum + sport1.Value + ".xml")

                                    queryGame = (From game In myUpdate.Descendants("score").Descendants("game")
                                             Select New ScoringUpdate With {.hometeam = game.Attribute("hometeam").Value,
                                                                           .homescore = game.Elements("homescore").Value,
                                                                           .awayteam = game.Attribute("awayteam").Value,
                                                                           .awayscore = game.Elements("awayscore").Value,
                                                                           .GameCode = game.Attribute("gamecode").Value,
                                                                           .GameDate = game.Elements("gamedate").Value,
                                                                           .gametime = game.Elements("gametime").Value,
                                                                           .DisplayStatus1 = game.Elements("display_status1").Value,
                                                                           .DisplayStatus2 = game.Elements("display_status2").Value,
                                                                           .Status = game.Elements("status").Value}).ToList

                                    For Each game In queryGame

                                        Dim querySchedule3 = (From game1 In _dbLoserPool5.ScheduleEntities
                                                                Where game1.CronJob = cronJobName And game1.GameCode = game.GameCode _
                                                                And game1.GameDate = game.GameDate).SingleOrDefault

                                        Dim queryUserPicks = (From pick1 In _dbLoserPool5.UserPicks
                                                              Where pick1.CronJobName = cronJobName And pick1.GameCode = game.GameCode).ToList


                                        If querySchedule3 Is Nothing And game.Status = "Pre Game" Then

                                        ElseIf Not querySchedule3 Is Nothing And game.Status = "Pre Game" Then

                                            querySchedule3.StartDate = game.GameDate
                                            querySchedule3.StartTime = game.gametime
                                            querySchedule3.GameDate = game.GameDate
                                            querySchedule3.GameTime = game.gametime
                                            querySchedule3.DisplayStatus1 = game.DisplayStatus1
                                            querySchedule3.DisplayStatus2 = game.DisplayStatus2
                                            querySchedule3.Status = game.Status
                                            querySchedule3.StartDateTime = DateTime.Parse(game.GameDate + " " + game.gametime)

                                            _dbLoserPool5.SaveChanges()

                                        ElseIf Not querySchedule3 Is Nothing And game.Status <> "Pre Game" Then

                                            If game.homescore Is Nothing Or game.homescore = "" Then
                                                querySchedule3.HomeScore = "0"
                                            Else
                                                querySchedule3.HomeScore = game.homescore
                                            End If

                                            If game.awayscore Is Nothing Or game.awayscore = "" Then
                                                querySchedule3.AwayScore = "0"
                                            Else
                                                querySchedule3.AwayScore = game.awayscore
                                            End If

                                            querySchedule3.GameDate = game.GameDate
                                            querySchedule3.GameTime = game.gametime
                                            querySchedule3.DisplayStatus1 = game.DisplayStatus1
                                            querySchedule3.DisplayStatus2 = game.DisplayStatus2
                                            querySchedule3.Status = game.Status

                                            Dim homescore = CInt(game.homescore)
                                            Dim awayscore = CInt(game.awayscore)

                                            If homescore = awayscore Then
                                                querySchedule3.WinningTeam = "tied"
                                                querySchedule3.IsHomeTeamWinning = False
                                                querySchedule3.AreTeamsTied = True
                                            ElseIf homescore > awayscore Then
                                                querySchedule3.WinningTeam = game.hometeam
                                                querySchedule3.IsHomeTeamWinning = True
                                                querySchedule3.AreTeamsTied = False
                                            ElseIf homescore < awayscore Then
                                                querySchedule3.WinningTeam = game.awayteam
                                                querySchedule3.IsHomeTeamWinning = False
                                                querySchedule3.AreTeamsTied = False
                                            End If

                                            _dbLoserPool5.SaveChanges()

                                            For Each user1 In queryUserPicks
                                                If user1.GameCode = game.GameCode Then
                                                    If homescore = awayscore Then
                                                        user1.PickIsTied = True
                                                        user1.PickIsWinning = False
                                                        _dbLoserPool5.SaveChanges()
                                                    ElseIf homescore > awayscore Then
                                                        If user1.UserPick1 = game.hometeam Then
                                                            user1.PickIsTied = False
                                                            user1.PickIsWinning = False
                                                            _dbLoserPool5.SaveChanges()
                                                        ElseIf user1.UserPick1 = game.awayteam Then
                                                            user1.PickIsTied = False
                                                            user1.PickIsWinning = True
                                                            _dbLoserPool5.SaveChanges()
                                                        End If
                                                    ElseIf homescore < awayscore Then
                                                        If user1.UserPick1 = game.hometeam Then
                                                            user1.PickIsTied = False
                                                            user1.PickIsWinning = True
                                                            _dbLoserPool5.SaveChanges()
                                                        ElseIf user1.UserPick1 = game.awayteam Then
                                                            user1.PickIsTied = False
                                                            user1.PickIsWinning = False
                                                            _dbLoserPool5.SaveChanges()
                                                        End If
                                                    End If
                                                End If
                                            Next
                                        End If
                                    Next

                                    Dim queryCronJobPools1 = (From qCJP1 In _dbApp5.CronJobPools
                                                              Where qCJP1.CronJobName = cronJobName).ToList

                                    For Each pool1 In queryCronJobPools1

                                        Dim queryUserChoices1 = (From qUC1 In _dbLoserPool5.UserChoicesList
                                                                    Where qUC1.CronJob = cronJobName And qUC1.TimePeriod = thisTimePeriod And _
                                                                    qUC1.PoolAlias = pool1.CronJobPoolAlias And qUC1.Contender = True).ToList

                                        For Each user1 In queryUserChoices1
                                            Dim queryUserPicks1 = (From qUP1 In _dbLoserPool5.UserPicks
                                                                     Where qUP1.PoolAlias = pool1.CronJobPoolAlias And qUP1.UserID = user1.UserID).ToList

                                            Dim userIsWinning = False
                                            Dim userIsTied = True

                                            For Each pick1 In queryUserPicks1
                                                If pick1.PickIsTied = True Then
                                                ElseIf pick1.PickIsWinning = True Then
                                                    userIsTied = False
                                                    userIsWinning = True
                                                ElseIf pick1.PickIsWinning = False Then
                                                    userIsTied = False
                                                    userIsWinning = False
                                                    Exit For
                                                End If
                                            Next

                                            user1.UserIsWinning = userIsWinning
                                            user1.UserIsTied = userIsTied

                                            _dbLoserPool5.SaveChanges()
                                        Next
                                    Next

                                    allGamesAreFinal = True

                                    For Each game In queryGame
                                        If game.Status <> "Postponed" And game.Status <> "final" Then
                                            allGamesAreFinal = False
                                        End If
                                    Next

                                    FinalizeUpdateToDatabase(queryGame, myUpdate, sport1.Value, cronJobName)

                                    If allGamesAreFinal = True Then

                                        Dim queryScoringUpdate = (From sU1 In _dbLoserPool5.CurrentScoringUpdates
                                                                  Where sU1.CronJobName = cronJobName).ToList

                                        Dim nextTimePeriod = queryPoolParam1(0).timePeriodName + CStr(CInt(cnt) + CInt(queryPoolParam1(0).timePeriodIncrement))
                                        Dim queryTimePeriod1 = (From timePeriod2 In _dbLoserPool5.ScheduleTimePeriods
                                                        Where (timePeriod2.TimePeriod = thisTimePeriod Or timePeriod2.TimePeriod = nextTimePeriod) And timePeriod2.CronJob = cronJobName
                                                        Order By timePeriod2.TimePeriod).ToList

                                        queryTimePeriod1(0).TimePeriodEndDate = queryScoringUpdate(0).filedate
                                        queryTimePeriod1(0).TimePeriodEndTime = queryScoringUpdate(0).filetime

                                        If queryTimePeriod1.Count > 1 Then
                                            queryTimePeriod1(1).TimePeriodStartDate = queryScoringUpdate(0).filedate
                                            queryTimePeriod1(1).TimePeriodStartTime = queryScoringUpdate(0).filetime
                                        End If

                                        _dbLoserPool5.SaveChanges()

                                        For Each pool1 In queryCronJobPools
                                            ContenderStatus.UpdateContenderStatus(thisTimePeriod, queryScoringUpdate, thisTimePeriod, CInt(queryPoolParam1(0).timePeriodIncrement), pool1.CronJobPoolAlias, teams1, sport1.Value, queryPoolParam1(0).timePeriodName, Me.cronJobName)
                                        Next

                                    End If
                                Next

                                If allGamesAreFinal = True Then
                                    Exit For
                                End If

                                Thread.Sleep(TimeSpan.FromMinutes(Me.TD3))
                            Next

                            For Each sport1a In Sports1

                                Dim queryPoolParam2 = (From poolParam1 In _dbPools5.PoolParameters
                                       Where poolParam1.CronJob = cronJobName).ToList

                                For Each pool1 In queryPoolParam2
                                    pool1.poolState = "Enter Picks"
                                    pool1.TimePeriod = pool1.timePeriodName + CStr(1 + (timePeriodCnt) * CInt(pool1.timePeriodIncrement))
                                    _dbPools5.SaveChanges()

                                    Dim dayNum = CInt(Mid(pool1.TimePeriod, Len(pool1.timePeriodName) + 1))
                                    If dayNum > queryPoolParam2(0).maxTimePeriod Then
                                        SeasonHasEnded = True
                                        queryPoolParam2(0).TimePeriod = queryPoolParam2(0).timePeriodName + CStr(queryPoolParam2(0).maxTimePeriod)
                                        _dbPools5.SaveChanges()
                                    End If
                                Next
                            Next

                            If SeasonHasEnded = True Then
                                Exit For
                            End If
                            Thread.Sleep(TimeSpan.FromMinutes(Me.TD1))
                        Next

                        For Each sport1a In Sports1
                            Dim queryPoolParam6 = (From poolParam6 In _dbPools5.PoolParameters
                               Where poolParam6.CronJob = cronJobName).ToList

                            For Each pool1 In queryPoolParam6
                                pool1.poolState = "Season End"
                            Next
                            _dbPools5.SaveChanges()
                        Next

                    End Using
                End Using
            End Using
        Catch ex As Exception

        End Try

    End Sub

    Private Sub FinalizeUpdateToDatabase(queryGame As List(Of ScoringUpdate), myUpdate As XDocument, sport As String, cronJobName As String)

        Dim _dbLoserPool8 As New LosersPoolContext
        Try
            Using (_dbLoserPool8)

                Dim queryCurrentScoringUpdates = (From update1 In _dbLoserPool8.CurrentScoringUpdates
                                                  Where update1.CronJobName = cronJobName).ToList

                If queryCurrentScoringUpdates.Count > 0 Then
                    For Each update1 In queryCurrentScoringUpdates
                        _dbLoserPool8.CurrentScoringUpdates.Remove(update1)
                    Next
                End If

                Dim queryTime1 = (From score In myUpdate.Descendants("score")
                Select New ScoringUpdate With {.filetime = score.Attribute("filetime"),
                                                .filedate = score.Attribute("filedate"),
                                                .TimePeriod = score.Attribute("TimePeriod")}).Single

                Dim cnt As Int16 = 1
                Dim ReorderGames As Boolean = False
                For Each game1 In queryGame

                    Dim querySchedule1 = (From qS1 In _dbLoserPool8.ScheduleEntities
                                          Where qS1.CronJob = cronJobName And qS1.GameCode = game1.GameCode).SingleOrDefault



                    If game1.Status = "Postponed" Then

                        Dim queryPostponedGames = (From qPG1 In _dbLoserPool8.PostponedGames
                                                   Where qPG1.CronJobName = cronJobName And qPG1.GameCode = game1.GameCode And qPG1.TimePeriod = queryTime1.TimePeriod).SingleOrDefault

                        If queryPostponedGames Is Nothing Then

                            Dim queryUserChoices = (From qUC1 In _dbLoserPool8.UserChoicesList
                                                    Where qUC1.Contender = True And qUC1.PickedGameCode = game1.GameCode And qUC1.TimePeriod = queryTime1.TimePeriod).ToList

                            For Each user1 In queryUserChoices
                                user1.UserPickPostponed = True

                                Dim queryUserPick = (From qUP1 In _dbLoserPool8.UserPicks
                                                     Where qUP1.PoolAlias = user1.PoolAlias And qUP1.TimePeriod = queryTime1.TimePeriod And qUP1.GameCode = game1.GameCode).ToList

                                For Each pick1 In queryUserPick
                                    pick1.UserPickPostponed = True
                                    _dbLoserPool8.SaveChanges()
                                Next
                            Next

                            Dim pG1 As New PostponedGame

                            pG1.filedate = queryTime1.filedate
                            pG1.filetime = queryTime1.filetime
                            pG1.TimePeriod = queryTime1.TimePeriod
                            pG1.gameId = "game" + CStr(cnt)
                            pG1.GameCode = game1.GameCode
                            pG1.GameDate = game1.GameDate
                            pG1.gametime = game1.gametime
                            pG1.hometeam = game1.hometeam
                            pG1.awayteam = game1.awayteam
                            pG1.homescore = game1.homescore
                            pG1.awayscore = game1.awayscore

                            If querySchedule1 Is Nothing Then
                            Else

                                Dim dG1 As New DeletedGame

                                dG1.TimePeriod = queryTime1.TimePeriod
                                dG1.GameId = "game" + CStr(cnt)
                                dG1.GameCode = game1.GameCode
                                dG1.GameDate = game1.GameDate
                                dG1.GameTime = game1.gametime
                                dG1.HomeTeam = game1.hometeam
                                dG1.AwayTeam = game1.awayteam
                                dG1.HomeScore = game1.homescore
                                dG1.AwayScore = game1.awayscore
                                dG1.OriginalStartDate = querySchedule1.OriginalStartDate
                                dG1.OriginalStartTime = querySchedule1.OriginalStartTime
                                dG1.DisplayStatus1 = game1.DisplayStatus1
                                dG1.DisplayStatus2 = game1.DisplayStatus2
                                dG1.StartDateTime = DateTime.Parse(game1.GameDate + " " + game1.gametime)
                                dG1.RescheduledGame = querySchedule1.RescheduledGame
                                dG1.Sport = game1.Sport
                                dG1.Status = game1.Status
                                dG1.CronJob = cronJobName
                                dG1.WinningTeam = querySchedule1.WinningTeam
                                dG1.IsHomeTeamWinning = querySchedule1.IsHomeTeamWinning
                                dG1.AreTeamsTied = querySchedule1.AreTeamsTied

                                _dbLoserPool8.DeletedGames.Add(dG1)

                            End If

                            pG1.DisplayStatus1 = game1.DisplayStatus1
                            pG1.DisplayStatus2 = game1.DisplayStatus2

                            pG1.Sport = game1.Sport
                            pG1.Status = game1.Status
                            pG1.CronJobName = cronJobName

                            cnt = cnt + 1

                            _dbLoserPool8.PostponedGames.Add(pG1)
                            _dbLoserPool8.ScheduleEntities.Remove(querySchedule1)
                            _dbLoserPool8.SaveChanges()
                        End If

                    ElseIf game1.Status <> "Postponed" Then

                        Dim cSU1 As New CurrentScoringUpdate
                        Dim cSU2 As New ScoringUpdate

                        cSU1.filedate = queryTime1.filedate
                        cSU2.filedate = queryTime1.filedate
                        cSU1.filetime = queryTime1.filetime
                        cSU2.filetime = queryTime1.filetime
                        cSU1.TimePeriod = queryTime1.TimePeriod
                        cSU2.TimePeriod = queryTime1.TimePeriod

                        cSU1.gameId = "game" + CStr(cnt)
                        cSU2.gameId = "game" + CStr(cnt)

                        cSU1.hometeam = game1.hometeam
                        cSU2.hometeam = game1.hometeam
                        cSU1.awayteam = game1.awayteam
                        cSU2.awayteam = game1.awayteam
                        cSU1.homescore = game1.homescore
                        cSU2.homescore = game1.homescore
                        cSU1.awayscore = game1.awayscore
                        cSU2.awayscore = game1.awayscore
                        cSU1.gametime = game1.gametime
                        cSU2.gametime = game1.gametime
                        cSU1.GameDate = game1.GameDate
                        cSU2.GameDate = game1.GameDate
                        cSU1.GameCode = game1.GameCode
                        cSU2.GameCode = game1.GameCode

                        If querySchedule1 Is Nothing Then

                            Dim queryDeletedGame = (From qDG1 In _dbLoserPool8.DeletedGames
                                                    Where qDG1.CronJob = cronJobName And qDG1.GameCode = game1.GameCode).SingleOrDefault

                            Dim team1 = queryDeletedGame.HomeTeam
                            Dim team2 = queryDeletedGame.AwayTeam

                            Dim queryGameSchedule = (From qGS1 In _dbLoserPool8.ScheduleEntities
                                                     Where qGS1.CronJob = cronJobName And ((qGS1.HomeTeam = team1 And qGS1.AwayTeam = team2) Or _
                                                                                           (qGS1.HomeTeam = team2 And qGS1.AwayTeam = team1))).SingleOrDefault

                            If queryDeletedGame Is Nothing And game1.Status = "Pre Game" Then

                            Else

                                ReorderGames = True
                                Dim scheduleGame1 As New ScheduleEntity

                                scheduleGame1.CronJob = queryDeletedGame.CronJob
                                scheduleGame1.GameCode = queryDeletedGame.GameCode
                                scheduleGame1.OriginalStartDate = queryDeletedGame.OriginalStartDate
                                scheduleGame1.OriginalStartTime = queryDeletedGame.OriginalStartTime
                                scheduleGame1.HomeTeam = queryDeletedGame.HomeTeam
                                scheduleGame1.HomeScore = queryDeletedGame.HomeScore
                                scheduleGame1.AwayTeam = queryDeletedGame.AwayTeam
                                scheduleGame1.AwayScore = queryDeletedGame.AwayScore
                                scheduleGame1.Sport = queryDeletedGame.Sport
                                scheduleGame1.GameDate = game1.GameDate
                                scheduleGame1.GameTime = game1.gametime
                                scheduleGame1.StartDate = game1.GameDate
                                scheduleGame1.StartTime = game1.gametime
                                scheduleGame1.DisplayStatus1 = game1.DisplayStatus1
                                scheduleGame1.DisplayStatus2 = game1.DisplayStatus2
                                scheduleGame1.Status = game1.Status
                                scheduleGame1.TimePeriod = queryTime1.TimePeriod
                                scheduleGame1.StartDateTime = DateTime.Parse(scheduleGame1.StartDate + " " + scheduleGame1.StartTime)
                                scheduleGame1.RescheduledGame = True
                                scheduleGame1.WinningTeam = queryDeletedGame.WinningTeam
                                scheduleGame1.IsHomeTeamWinning = queryDeletedGame.IsHomeTeamWinning
                                scheduleGame1.AreTeamsTied = queryDeletedGame.AreTeamsTied

                                If queryGameSchedule Is Nothing Then
                                    scheduleGame1.MultipleGamesAreScheduled = False
                                Else
                                    scheduleGame1.MultipleGamesAreScheduled = True
                                    If scheduleGame1.StartDateTime < queryGameSchedule.StartDateTime Then
                                        scheduleGame1.MultipleGameNumber = "1"
                                        queryGameSchedule.MultipleGamesAreScheduled = True
                                        queryGameSchedule.MultipleGameNumber = "2"
                                    Else
                                        queryGameSchedule.MultipleGamesAreScheduled = True
                                        queryGameSchedule.MultipleGameNumber = "1"
                                        scheduleGame1.MultipleGameNumber = "2"
                                    End If
                                End If

                                _dbLoserPool8.ScheduleEntities.Add(scheduleGame1)


                            End If

                        Else

                        End If

                        cSU1.DisplayStatus1 = game1.DisplayStatus1
                        cSU2.DisplayStatus1 = game1.DisplayStatus1
                        cSU1.DisplayStatus2 = game1.DisplayStatus2
                        cSU2.DisplayStatus2 = game1.DisplayStatus2
                        cSU1.Status = game1.Status
                        cSU2.Status = game1.Status
                        cSU1.Sport = sport
                        cSU2.Sport = sport
                        cSU1.CronJobName = cronJobName
                        cSU2.CronJobName = cronJobName

                        cnt = cnt + 1

                        _dbLoserPool8.CurrentScoringUpdates.Add(cSU1)
                        _dbLoserPool8.ScoringUpdates.Add(cSU2)
                        _dbLoserPool8.SaveChanges()

                    End If


                Next

                If ReorderGames = True Then
                    Dim querySchedule2 = (From game1 In _dbLoserPool8.ScheduleEntities
                      Where game1.CronJob = cronJobName And game1.TimePeriod = queryTime1.TimePeriod
                      Order By game1.StartDateTime Ascending).ToList

                    cnt = 1
                    For Each game1 In querySchedule2
                        game1.GameId = "game" + CStr(cnt)
                        cnt = cnt + 1
                        _dbLoserPool8.SaveChanges()
                    Next

                    For Each game1 In querySchedule2
                        _dbLoserPool8.ScheduleEntities.Remove(game1)
                    Next
                    _dbLoserPool8.SaveChanges()

                    Thread.Sleep(2500)

                    For Each game1 In querySchedule2
                        _dbLoserPool8.ScheduleEntities.Add(game1)
                    Next
                    _dbLoserPool8.SaveChanges()

                End If

            End Using

        Catch ex As Exception

        End Try
    End Sub

End Class


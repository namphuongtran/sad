'use client';

import React, { useEffect, useState } from 'react';
import { Client } from '@stomp/stompjs';
import axios from 'axios';

interface User {
  rank: number;
  nickname: string;
  score: number;
}

interface LeaderboardData {
  lastModifiedDate: string;
  userList: User[];
}

const Leaderboard: React.FC = () => {
  const [leaderboard, setLeaderboard] = useState<LeaderboardData | null>(null);
  const [stompClient, setStompClient] = useState<Client | null>(null);

  useEffect(() => {
    const client = new Client({
      brokerURL: 'ws://localhost:5153/online-game',
    });

    client.onConnect = (frame) => {
      loadLeaderboardCache();
      console.log('Connected: ' + frame);
      client.subscribe('/live-updates/leaderboard_topic', (res) => {
        showLeaderboard(JSON.parse(res.body));
      });
    };

    client.onWebSocketError = (error) => {
      console.error('Error with websocket', error);
    };

    client.onStompError = (frame) => {
      console.error('Broker reported error: ' + frame.headers['message']);
      console.error('Additional details: ' + frame.body);
    };

    setStompClient(client);

    client.activate();
    console.log("ws connection established");

    return () => {
      client.deactivate();
      console.log("ws connection disconnected");
    };
  }, []);

  const loadLeaderboardCache = () => {
    const apiUrl = 'http://localhost:5153/leaderboard';

    axios.get(apiUrl)
      .then(response => {
        console.log('leaderboard cache:', response.data);
        showLeaderboard(response.data);
      })
      .catch(error => {
        console.error('Error during fetch:', error);
      });
  };

  const showLeaderboard = (data: LeaderboardData) => {
    setLeaderboard(data);
  };

  return (
    <div>
      {leaderboard && (
        <div>
          <div id="lastModifiedDate">{leaderboard.lastModifiedDate}</div>
          <table id="leaderboard">
            <thead>
              <tr>
                <th>rank</th>
                <th>username</th>
                <th>score</th>
              </tr>
            </thead>
            <tbody>
              {leaderboard.userList.map(user => (
                <tr key={user.rank}>
                  <td>{user.rank}</td>
                  <td>{user.nickname}</td>
                  <td>{user.score}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default Leaderboard;

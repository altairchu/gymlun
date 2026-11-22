import { Alert, Button, LinearProgress } from '@mui/material';
import { type FC, useState } from 'react';
import { useGetMcpConnectionsQuery, useRevokeMcpConnectionMutation } from '@/entities/gym/gymApi';
import { PageContainer } from '@/shared/ui';

const connectionPrompt =
  '請連接 GymLun MCP。完成 AtlasApple 授權後，先呼叫 get_gym_context 讀取我嘅目標、目前飲食方式同最新體重，再用呢啲資料幫我分析每日所需或者更新記錄。';

export const GymConnectionsPage: FC = () => {
  const { data, isLoading, isError, refetch } = useGetMcpConnectionsQuery();
  const [revoke, { isLoading: isRevoking }] = useRevokeMcpConnectionMutation();
  const [pending, setPending] = useState<string | null>(null);
  const [notice, setNotice] = useState<{ message: string; error: boolean } | null>(null);

  const copyPrompt = async (): Promise<void> => {
    try {
      await navigator.clipboard.writeText(
        `${connectionPrompt}\nMCP 網址：${data?.serverUrl ?? ''}`,
      );
      setNotice({ message: '已複製連接提示詞', error: false });
    } catch {
      setNotice({ message: '未能複製。可以選取提示詞同網址再複製。', error: true });
    }
  };

  const confirmRevoke = async (id: string): Promise<void> => {
    try {
      await revoke(id).unwrap();
      setPending(null);
      setNotice({ message: '已撤銷授權', error: false });
    } catch {
      setNotice({ message: '未能撤銷授權。請再試。', error: true });
    }
  };

  return (
    <PageContainer className="gym-feature-page">
      <h2>AI 連接</h2>
      {data?.connections.length === 0 && data.serverUrl ? (
        <p className="gym-muted">
          每次連接要用 AtlasApple 登入同授權。獲授權嘅應用可以完整讀寫你嘅 GymLun 資料。
        </p>
      ) : null}
      {isLoading ? <LinearProgress aria-label="讀取 AI 連接" /> : null}
      {isError ? (
        <Alert
          severity="error"
          action={
            <Button
              onClick={() => {
                void refetch();
              }}
            >
              重試
            </Button>
          }
        >
          未能讀取 AI 連接
        </Alert>
      ) : null}
      {data?.connections.length === 0 && data.serverUrl ? (
        <section className="gym-card gym-connect-instructions">
          <h3>連接 AI 應用</h3>
          <p className="gym-connect-prompt">{connectionPrompt}</p>
          <Button
            variant="contained"
            onClick={() => {
              void copyPrompt();
            }}
          >
            複製連接提示詞
          </Button>
          <details>
            <summary>手動設定</summary>
            <p>喺支援自訂 MCP 嘅 AI 應用加入以下網址，再用 AtlasApple 登入授權。</p>
          </details>
          <p className="gym-connect-address">
            <code>{data.serverUrl}</code>
          </p>
        </section>
      ) : null}
      {data?.connections.length === 0 && data.serverUrl === null ? (
        <Alert severity="info">呢個 GymLun 目前未開放 AI 連接。</Alert>
      ) : null}
      {notice ? (
        <Alert severity={notice.error ? 'error' : 'success'}>{notice.message}</Alert>
      ) : null}
      {data ? (
        <section className="gym-card">
          <h3>已授權嘅應用</h3>
          {data.connections.length === 0 ? <p className="gym-muted">暫未有已授權應用。</p> : null}
          <ul className="gym-connections">
            {data.connections.map(connection => (
              <li key={connection.id}>
                <div>
                  <strong>{connection.clientName}</strong>
                  <span>完整讀寫 · {connection.authorizedAt.slice(0, 10)}</span>
                </div>
                {pending === connection.id ? (
                  <div className="gym-action-row">
                    <Button onClick={() => setPending(null)}>取消</Button>
                    <Button
                      color="error"
                      disabled={isRevoking}
                      onClick={() => {
                        void confirmRevoke(connection.id);
                      }}
                    >
                      確認撤銷
                    </Button>
                  </div>
                ) : (
                  <Button color="error" onClick={() => setPending(connection.id)}>
                    撤銷
                  </Button>
                )}
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </PageContainer>
  );
};

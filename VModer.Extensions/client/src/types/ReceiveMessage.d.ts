interface ReceiveMessage<TData> {
	type: string;
	data: TData;
}
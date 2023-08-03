class IdMapper {
    private readonly strToNumMapping: Map<string, number> = new Map();
    private readonly numToStrMapping: Map<number, string> = new Map();

    public add = (id: string) => {
        const numId = this.generate();
        this.strToNumMapping.set(id, numId);
        this.numToStrMapping.set(numId, id);
    };

    private generate = () => {
        const now = new Date();
        return now.getTime() + this.strToNumMapping.size;
    };

    public has = (id: string | number) => {
        return Number.isFinite(id) ? this.numToStrMapping.has(id as number) : this.strToNumMapping.has(id as string);
    };

    public get = (id: string | number) => {
        return Number.isFinite(id) ? this.numToStrMapping.get(id as number) : this.strToNumMapping.get(id as string);
    };

    public remove = (id: string) => {
        if (!this.has(id)) {
            return;
        }
        const numId = this.strToNumMapping.get(id) as number;
        this.strToNumMapping.delete(id);
        this.numToStrMapping.delete(numId);
    };

    public clear = () => {
        this.strToNumMapping.clear();
        this.numToStrMapping.clear();
    };
}

export { IdMapper };
